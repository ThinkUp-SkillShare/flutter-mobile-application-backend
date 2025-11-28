using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.Models;

namespace SkillShareBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CallsController : ControllerBase
{
    private static readonly Dictionary<string, CallSession> _activeCalls = new();
    private readonly AppDbContext _context;

    public CallsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("create-room")]
    public async Task<IActionResult> CreateCallRoom([FromBody] CreateCallRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Verificar si el usuario es miembro del grupo
            var groupMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == request.GroupId && gm.UserId == userId);

            if (groupMember == null) return Unauthorized("You are not a member of this group");

            var group = await _context.StudyGroups.FindAsync(request.GroupId);
            if (group == null) return NotFound("Group not found");

            var callId = Guid.NewGuid().ToString();
            var callSession = new CallSession
            {
                CallId = callId,
                GroupId = request.GroupId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Participants = new List<SessionParticipant>
                {
                    new() { UserId = userId, JoinedAt = DateTime.UtcNow }
                }
            };

            _activeCalls[callId] = callSession;

            // Guardar en base de datos para historial
            var callRecord = new GroupCall
            {
                CallId = callId,
                GroupId = request.GroupId,
                StartedBy = userId,
                StartedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.GroupCalls.Add(callRecord);

            // También registrar al primer participante
            var participantRecord = new CallParticipant
            {
                CallId = callId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };
            _context.CallParticipants.Add(participantRecord);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                callId,
                success = true,
                message = "Call room created successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error creating call room: {ex.Message}" });
        }
    }

    [HttpPost("get-token")]
    public async Task<IActionResult> GetCallToken([FromBody] CreateCallRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Verificar si el usuario es miembro del grupo
            var groupMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == request.GroupId && gm.UserId == userId);

            if (groupMember == null) return Unauthorized("You are not a member of this group");

            // Para WebRTC simple, generamos un token básico
            var tokenData = new
            {
                userId,
                groupId = request.GroupId,
                timestamp = DateTime.UtcNow.Ticks,
                expires = DateTime.UtcNow.AddHours(1).Ticks
            };

            var token = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tokenData)));

            return Ok(new
            {
                token,
                stunServers = new[]
                {
                    new { urls = "stun:stun.l.google.com:19302" },
                    new { urls = "stun:stun1.l.google.com:19302" }
                },
                turnServers = new[] // Estos son opcionales para desarrollo
                {
                    new
                    {
                        urls = "turn:your-turn-server.com:3478",
                        username = "username",
                        credential = "password"
                    }
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error getting call token: {ex.Message}" });
        }
    }

    [HttpPost("end-call")]
    public async Task<IActionResult> EndCall([FromBody] EndCallRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Buscar la llamada activa para este grupo
            var activeCall = _activeCalls.Values.FirstOrDefault(c =>
                c.GroupId == request.GroupId && c.IsActive);

            if (activeCall != null)
            {
                activeCall.IsActive = false;
                activeCall.EndedAt = DateTime.UtcNow;

                // Actualizar en base de datos
                var callRecord = await _context.GroupCalls
                    .FirstOrDefaultAsync(c => c.CallId == activeCall.CallId && c.IsActive);

                if (callRecord != null)
                {
                    callRecord.IsActive = false;
                    callRecord.EndedAt = DateTime.UtcNow;

                    // Actualizar participantes para marcar que salieron
                    var activeParticipants = await _context.CallParticipants
                        .Where(cp => cp.CallId == activeCall.CallId && cp.LeftAt == null)
                        .ToListAsync();

                    foreach (var participant in activeParticipants) participant.LeftAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }

                _activeCalls.Remove(activeCall.CallId);
            }

            return Ok(new { success = true, message = "Call ended successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error ending call: {ex.Message}" });
        }
    }

    [HttpPost("join-call")]
    public async Task<IActionResult> JoinCall([FromBody] JoinCallRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Verificar si existe una llamada activa
            var activeCall = _activeCalls.Values.FirstOrDefault(c =>
                c.GroupId == request.GroupId && c.IsActive);

            if (activeCall == null) return NotFound(new { error = "No active call found for this group" });

            // Verificar si el usuario es miembro del grupo
            var groupMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == request.GroupId && gm.UserId == userId);

            if (groupMember == null) return Unauthorized("You are not a member of this group");

            // Agregar usuario a la sesión en memoria
            if (!activeCall.Participants.Any(p => p.UserId == userId))
                activeCall.Participants.Add(new SessionParticipant
                {
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                });

            // Registrar en base de datos
            var existingParticipant = await _context.CallParticipants
                .FirstOrDefaultAsync(cp => cp.CallId == activeCall.CallId && cp.UserId == userId && cp.LeftAt == null);

            if (existingParticipant == null)
            {
                var participantRecord = new CallParticipant
                {
                    CallId = activeCall.CallId,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                };
                _context.CallParticipants.Add(participantRecord);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                callId = activeCall.CallId,
                success = true,
                message = "Joined call successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error joining call: {ex.Message}" });
        }
    }

    [HttpGet("active-call/{groupId}")]
    public async Task<IActionResult> GetActiveCall(int groupId)
    {
        try
        {
            var activeCall = _activeCalls.Values.FirstOrDefault(c =>
                c.GroupId == groupId && c.IsActive);

            if (activeCall == null) return NotFound(new { error = "No active call found" });

            return Ok(new
            {
                callId = activeCall.CallId,
                createdBy = activeCall.CreatedBy,
                participantCount = activeCall.Participants.Count,
                isActive = activeCall.IsActive,
                participants = activeCall.Participants.Select(p => new
                {
                    userId = p.UserId,
                    joinedAt = p.JoinedAt // CORREGIDO: 'JointedAt' → 'JoinedAt'
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error getting active call: {ex.Message}" });
        }
    }

    [HttpGet("call-history/{groupId}")]
    public async Task<IActionResult> GetCallHistory(int groupId)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Verificar si el usuario es miembro del grupo
            var groupMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (groupMember == null) return Unauthorized("You are not a member of this group");

            var callHistory = await _context.GroupCalls
                .Where(gc => gc.GroupId == groupId)
                .OrderByDescending(gc => gc.StartedAt)
                .Select(gc => new
                {
                    callId = gc.CallId,
                    startedBy = gc.StartedBy,
                    startedAt = gc.StartedAt,
                    endedAt = gc.EndedAt,
                    duration = gc.Duration,
                    participantCount = _context.CallParticipants
                        .Count(cp => cp.CallId == gc.CallId)
                })
                .ToListAsync();

            return Ok(callHistory);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error getting call history: {ex.Message}" });
        }
    }
}

// Clases auxiliares para el controlador (NO son modelos de BD)
public class CreateCallRequest
{
    public int GroupId { get; set; }
}

public class EndCallRequest
{
    public int GroupId { get; set; }
}

public class JoinCallRequest
{
    public int GroupId { get; set; }
}

public class CallSession
{
    public string CallId { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public List<SessionParticipant> Participants { get; set; } = new();
}

public class SessionParticipant
{
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
}