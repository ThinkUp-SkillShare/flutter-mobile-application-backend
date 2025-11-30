using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.Models;

namespace SkillShareBackend.Services;

public class CallService : ICallService
{
    private readonly AppDbContext _context;
    private static readonly ConcurrentDictionary<string, CallSession> _activeCalls = new();
    private static readonly ConcurrentDictionary<int, string> _groupToCallMapping = new();

    public CallService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CallRoomResult> CreateCallRoom(int groupId, string userId)
    {
        try
        {
            // Verificar si el grupo existe y el usuario es miembro
            var group = await _context.StudyGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return new CallRoomResult 
                { 
                    Success = false, 
                    Message = "Group not found" 
                };
            }

            var isMember = group.Members.Any(m => m.UserId.ToString() == userId);
            if (!isMember)
            {
                return new CallRoomResult 
                { 
                    Success = false, 
                    Message = "User is not a member of this group" 
                };
            }

            // Verificar si ya existe una llamada activa para este grupo
            if (_groupToCallMapping.TryGetValue(groupId, out var existingCallId))
            {
                if (_activeCalls.TryGetValue(existingCallId, out var existingSession) && existingSession.IsActive)
                {
                    Console.WriteLine($"✅ Found existing active call: {existingCallId} for group: {groupId}");
                    
                    // Agregar usuario a participantes si no está
                    if (!existingSession.Participants.Contains(userId))
                    {
                        existingSession.Participants.Add(userId);
                    }

                    return new CallRoomResult 
                    { 
                        Success = true, 
                        CallId = existingCallId,
                        Message = "Joined existing call" 
                    };
                }
            }

            // Crear nueva llamada
            var callId = Guid.NewGuid().ToString();
            var callSession = new CallSession
            {
                CallId = callId,
                GroupId = groupId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Participants = new List<string> { userId }
            };

            _activeCalls[callId] = callSession;
            _groupToCallMapping[groupId] = callId;

            // Registrar en la base de datos
            var callRecord = new Call
            {
                CallId = callId,
                GroupId = groupId,
                StartedBy = userId,
                StartedAt = DateTime.UtcNow,
                IsActive = true,
                ParticipantCount = 1
            };

            _context.GroupCalls.Add(callRecord);

            // Registrar participante
            var participant = new CallParticipant
            {
                CallId = callId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };

            _context.CallParticipants.Add(participant);
            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ Call room created: {callId} for group: {groupId} by user: {userId}");

            return new CallRoomResult 
            { 
                Success = true, 
                CallId = callId,
                Message = "Call room created successfully" 
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating call room: {ex.Message}");
            return new CallRoomResult 
            { 
                Success = false, 
                Message = $"Error creating call room: {ex.Message}" 
            };
        }
    }

    public async Task<JoinCallResult> JoinCall(int groupId, string userId)
    {
        try
        {
            // Verificar que el usuario sea miembro del grupo
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId.ToString() == userId);

            if (!isMember)
            {
                return new JoinCallResult 
                { 
                    Success = false, 
                    Message = "User is not a member of this group" 
                };
            }

            // Buscar llamada activa
            if (!_groupToCallMapping.TryGetValue(groupId, out var callId))
            {
                return new JoinCallResult 
                { 
                    Success = false, 
                    Message = "No active call found for this group" 
                };
            }

            if (!_activeCalls.TryGetValue(callId, out var activeCall) || !activeCall.IsActive)
            {
                return new JoinCallResult 
                { 
                    Success = false, 
                    Message = "No active call found for this group" 
                };
            }

            // Agregar usuario a participantes
            if (!activeCall.Participants.Contains(userId))
            {
                activeCall.Participants.Add(userId);
            }

            // Actualizar en base de datos
            var callRecord = await _context.GroupCalls
                .FirstOrDefaultAsync(c => c.CallId == callId && c.IsActive);

            if (callRecord != null)
            {
                callRecord.ParticipantCount = activeCall.Participants.Count;

                // Verificar si el participante ya está registrado
                var existingParticipant = await _context.CallParticipants
                    .FirstOrDefaultAsync(cp => cp.CallId == callId && cp.UserId == userId && cp.LeftAt == null);

                if (existingParticipant == null)
                {
                    var participant = new CallParticipant
                    {
                        CallId = callId,
                        UserId = userId,
                        JoinedAt = DateTime.UtcNow
                    };
                    _context.CallParticipants.Add(participant);
                }

                await _context.SaveChangesAsync();
            }

            Console.WriteLine($"✅ User {userId} joined call: {callId} for group: {groupId}");

            return new JoinCallResult 
            { 
                Success = true, 
                CallId = callId,
                Message = "Joined call successfully" 
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error joining call: {ex.Message}");
            return new JoinCallResult 
            { 
                Success = false, 
                Message = $"Error joining call: {ex.Message}" 
            };
        }
    }

    public async Task<ActiveCallResult> GetActiveCall(int groupId)
    {
        try
        {
            // Buscar en memoria primero
            if (_groupToCallMapping.TryGetValue(groupId, out var callId))
            {
                if (_activeCalls.TryGetValue(callId, out var activeCall) && activeCall.IsActive)
                {
                    return new ActiveCallResult 
                    { 
                        Success = true,
                        CallId = callId,
                        IsActive = true,
                        ParticipantCount = activeCall.Participants.Count,
                        Message = "Active call found"
                    };
                }
            }

            // Buscar en base de datos
            var dbCall = await _context.GroupCalls
                .FirstOrDefaultAsync(c => c.GroupId == groupId && c.IsActive);

            if (dbCall != null)
            {
                // Sincronizar con memoria
                var callSession = new CallSession
                {
                    CallId = dbCall.CallId,
                    GroupId = groupId,
                    CreatedBy = dbCall.StartedBy,
                    CreatedAt = dbCall.StartedAt,
                    IsActive = true,
                    Participants = new List<string> { dbCall.StartedBy }
                };

                _activeCalls[dbCall.CallId] = callSession;
                _groupToCallMapping[groupId] = dbCall.CallId;

                return new ActiveCallResult 
                { 
                    Success = true,
                    CallId = dbCall.CallId,
                    IsActive = true,
                    ParticipantCount = dbCall.ParticipantCount,
                    Message = "Active call found in database"
                };
            }

            return new ActiveCallResult 
            { 
                Success = false,
                IsActive = false,
                Message = "No active call found"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting active call: {ex.Message}");
            return new ActiveCallResult 
            { 
                Success = false,
                IsActive = false,
                Message = $"Error getting active call: {ex.Message}"
            };
        }
    }

    public async Task<bool> EndCall(int groupId, string userId)
    {
        try
        {
            if (!_groupToCallMapping.TryGetValue(groupId, out var callId))
            {
                return false;
            }

            if (!_activeCalls.TryGetValue(callId, out var activeCall))
            {
                return false;
            }

            // Marcar como inactiva
            activeCall.IsActive = false;
            activeCall.EndedAt = DateTime.UtcNow;

            // Remover del diccionario
            _activeCalls.TryRemove(callId, out _);
            _groupToCallMapping.TryRemove(groupId, out _);

            // Actualizar base de datos
            var callRecord = await _context.GroupCalls
                .FirstOrDefaultAsync(c => c.CallId == callId && c.IsActive);

            if (callRecord != null)
            {
                callRecord.IsActive = false;
                callRecord.EndedAt = DateTime.UtcNow;
                callRecord.EndedBy = userId;

                // Cerrar todas las sesiones de participantes
                var activeParticipants = await _context.CallParticipants
                    .Where(cp => cp.CallId == callId && cp.LeftAt == null)
                    .ToListAsync();

                foreach (var participant in activeParticipants)
                {
                    participant.LeftAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            // Limpiar WebSocket
            WebSocketHandler.CleanupCall(callId);

            Console.WriteLine($"✅ Call ended: {callId} for group: {groupId} by user: {userId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error ending call: {ex.Message}");
            return false;
        }
    }

    public async Task<CallStatsResult> GetCallStats(int groupId)
    {
        try
        {
            var calls = await _context.GroupCalls
                .Where(c => c.GroupId == groupId)
                .OrderByDescending(c => c.StartedAt)
                .ToListAsync();

            if (!calls.Any())
            {
                return new CallStatsResult 
                { 
                    Success = false, 
                    Message = "No call history found for this group" 
                };
            }

            var totalParticipants = calls.Sum(c => c.ParticipantCount);
            var callsWithDuration = calls.Where(c => c.Duration.HasValue).ToList();
            var totalDuration = TimeSpan.FromSeconds(callsWithDuration.Sum(c => c.Duration!.Value.TotalSeconds));
            var averageDuration = callsWithDuration.Any() 
                ? (int)callsWithDuration.Average(c => c.Duration!.Value.TotalSeconds) 
                : 0;

            return new CallStatsResult
            {
                Success = true,
                TotalCalls = calls.Count,
                TotalParticipants = totalParticipants,
                AverageDuration = averageDuration,
                TotalDuration = totalDuration,
                Message = $"Found {calls.Count} call(s) for this group"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting call stats: {ex.Message}");
            return new CallStatsResult 
            { 
                Success = false, 
                Message = $"Error getting call stats: {ex.Message}" 
            };
        }
    }

    public async Task<UserCallStatsResult> GetUserCallStats(string userId)
    {
        try
        {
            var userParticipations = await _context.CallParticipants
                .Where(cp => cp.UserId == userId)
                .ToListAsync();

            if (!userParticipations.Any())
            {
                return new UserCallStatsResult 
                { 
                    Success = false, 
                    Message = "No call history found for this user" 
                };
            }

            var participationsWithDuration = userParticipations.Where(p => p.Duration.HasValue).ToList();
            var totalCallTime = TimeSpan.FromSeconds(participationsWithDuration.Sum(p => p.Duration!.Value.TotalSeconds));

            return new UserCallStatsResult
            {
                Success = true,
                TotalCalls = userParticipations.Count,
                TotalCallTime = totalCallTime,
                Message = $"User has participated in {userParticipations.Count} call(s)"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting user call stats: {ex.Message}");
            return new UserCallStatsResult 
            { 
                Success = false, 
                Message = $"Error getting user call stats: {ex.Message}" 
            };
        }
    }
}