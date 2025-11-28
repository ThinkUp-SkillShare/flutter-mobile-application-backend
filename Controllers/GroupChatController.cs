using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;

namespace SkillShareBackend.Controllers;

[ApiController]
[Route("api/groups/{groupId}/chat")]
[Authorize]
public class GroupChatController : ControllerBase
{
    private readonly AppDbContext _context;

    public GroupChatController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    // GET: api/groups/{groupId}/chat/messages
    [HttpGet("messages")]
    public async Task<ActionResult<IEnumerable<GroupMessageDto>>> GetMessages(
        int groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        // Verify user is a member
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
            return Forbid();

        var messages = await _context.GroupMessages
            .Where(m => m.GroupId == groupId && !m.IsDeleted)
            .Include(m => m.User)
            .Include(m => m.ReplyToMessage)
            .ThenInclude(rm => rm!.User)
            .Include(m => m.Reactions)
            .ThenInclude(r => r.User)
            .Include(m => m.ReadStatuses)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new GroupMessageDto
            {
                Id = m.Id,
                GroupId = m.GroupId,
                UserId = m.UserId,
                UserEmail = m.User!.Email,
                UserProfileImage = m.User.ProfileImage,
                MessageType = m.MessageType,
                Content = m.Content,
                FileUrl = m.FileUrl,
                FileName = m.FileName,
                FileSize = m.FileSize,
                Duration = m.Duration,
                ReplyToMessageId = m.ReplyToMessageId,
                ReplyToMessage = m.ReplyToMessage != null
                    ? new ReplyMessageDto
                    {
                        Id = m.ReplyToMessage.Id,
                        UserId = m.ReplyToMessage.UserId,
                        UserEmail = m.ReplyToMessage.User!.Email,
                        MessageType = m.ReplyToMessage.MessageType,
                        Content = m.ReplyToMessage.Content,
                        FileName = m.ReplyToMessage.FileName
                    }
                    : null,
                IsEdited = m.IsEdited,
                IsDeleted = m.IsDeleted,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                Reactions = m.Reactions.Select(r => new MessageReactionDto
                {
                    Id = r.Id,
                    MessageId = r.MessageId,
                    UserId = r.UserId,
                    UserEmail = r.User!.Email,
                    Reaction = r.Reaction,
                    CreatedAt = r.CreatedAt
                }).ToList(),
                IsRead = m.ReadStatuses.Any(rs => rs.UserId == userId),
                IsSentByCurrentUser = m.UserId == userId
            })
            .ToListAsync();

        return Ok(messages.OrderBy(m => m.CreatedAt));
    }

    // POST: api/groups/{groupId}/chat/messages
    [HttpPost("messages")]
    public async Task<ActionResult<GroupMessageDto>> SendMessage(
        int groupId,
        [FromBody] SendMessageDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        // Verify user is a member
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
            return Forbid();

        var message = new GroupMessage
        {
            GroupId = groupId,
            UserId = userId,
            MessageType = dto.MessageType,
            Content = dto.Content,
            FileUrl = dto.FileUrl,
            FileName = dto.FileName,
            FileSize = dto.FileSize,
            Duration = dto.Duration,
            ReplyToMessageId = dto.ReplyToMessageId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GroupMessages.Add(message);
        await _context.SaveChangesAsync();

        // Load relationships
        await _context.Entry(message).Reference(m => m.User).LoadAsync();
        if (message.ReplyToMessageId.HasValue)
        {
            await _context.Entry(message).Reference(m => m.ReplyToMessage).LoadAsync();
            if (message.ReplyToMessage != null)
                await _context.Entry(message.ReplyToMessage).Reference(m => m.User).LoadAsync();
        }

        var messageDto = new GroupMessageDto
        {
            Id = message.Id,
            GroupId = message.GroupId,
            UserId = message.UserId,
            UserEmail = message.User!.Email,
            UserProfileImage = message.User.ProfileImage,
            MessageType = message.MessageType,
            Content = message.Content,
            FileUrl = message.FileUrl,
            FileName = message.FileName,
            FileSize = message.FileSize,
            Duration = message.Duration,
            ReplyToMessageId = message.ReplyToMessageId,
            ReplyToMessage = message.ReplyToMessage != null
                ? new ReplyMessageDto
                {
                    Id = message.ReplyToMessage.Id,
                    UserId = message.ReplyToMessage.UserId,
                    UserEmail = message.ReplyToMessage.User!.Email,
                    MessageType = message.ReplyToMessage.MessageType,
                    Content = message.ReplyToMessage.Content,
                    FileName = message.ReplyToMessage.FileName
                }
                : null,
            IsEdited = message.IsEdited,
            IsDeleted = message.IsDeleted,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            Reactions = new List<MessageReactionDto>(),
            IsRead = false,
            IsSentByCurrentUser = true
        };

        return CreatedAtAction(nameof(GetMessages), new { groupId }, messageDto);
    }

    // PUT: api/groups/{groupId}/chat/messages/{messageId}
    [HttpPut("messages/{messageId}")]
    public async Task<IActionResult> UpdateMessage(
        int groupId,
        int messageId,
        [FromBody] UpdateMessageDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var message = await _context.GroupMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.GroupId == groupId);

        if (message == null)
            return NotFound();

        if (message.UserId != userId)
            return Forbid();

        if (dto.Content != null)
        {
            message.Content = dto.Content;
            message.IsEdited = true;
            message.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/groups/{groupId}/chat/messages/{messageId}
    [HttpDelete("messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(int groupId, int messageId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var message = await _context.GroupMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.GroupId == groupId);

        if (message == null)
            return NotFound();

        // Check if user is the message author or group admin
        var isAdmin = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.Role == "admin");

        if (message.UserId != userId && !isAdmin)
            return Forbid();

        message.IsDeleted = true;
        message.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST: api/groups/{groupId}/chat/messages/{messageId}/reactions
    [HttpPost("messages/{messageId}/reactions")]
    public async Task<ActionResult<MessageReactionDto>> AddReaction(
        int groupId,
        int messageId,
        [FromBody] AddReactionDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var message = await _context.GroupMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.GroupId == groupId);

        if (message == null)
            return NotFound();

        // Check if reaction already exists
        var existingReaction = await _context.MessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.Reaction == dto.Reaction);

        if (existingReaction != null)
            return BadRequest(new { message = "Reaction already exists" });

        var reaction = new MessageReaction
        {
            MessageId = messageId,
            UserId = userId,
            Reaction = dto.Reaction,
            CreatedAt = DateTime.UtcNow
        };

        _context.MessageReactions.Add(reaction);
        await _context.SaveChangesAsync();

        await _context.Entry(reaction).Reference(r => r.User).LoadAsync();

        var reactionDto = new MessageReactionDto
        {
            Id = reaction.Id,
            MessageId = reaction.MessageId,
            UserId = reaction.UserId,
            UserEmail = reaction.User!.Email,
            Reaction = reaction.Reaction,
            CreatedAt = reaction.CreatedAt
        };

        return Ok(reactionDto);
    }

    // DELETE: api/groups/{groupId}/chat/messages/{messageId}/reactions/{reactionId}
    [HttpDelete("messages/{messageId}/reactions/{reactionId}")]
    public async Task<IActionResult> RemoveReaction(
        int groupId,
        int messageId,
        int reactionId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var reaction = await _context.MessageReactions
            .FirstOrDefaultAsync(r => r.Id == reactionId && r.MessageId == messageId);

        if (reaction == null)
            return NotFound();

        if (reaction.UserId != userId)
            return Forbid();

        _context.MessageReactions.Remove(reaction);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/groups/{groupId}/chat/messages/{messageId}/read
    [HttpPost("messages/{messageId}/read")]
    public async Task<IActionResult> MarkAsRead(int groupId, int messageId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var message = await _context.GroupMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.GroupId == groupId);

        if (message == null)
            return NotFound();

        var existingStatus = await _context.MessageReadStatuses
            .FirstOrDefaultAsync(rs => rs.MessageId == messageId && rs.UserId == userId);

        if (existingStatus != null)
            return Ok();

        var readStatus = new MessageReadStatus
        {
            MessageId = messageId,
            UserId = userId,
            ReadAt = DateTime.UtcNow
        };

        _context.MessageReadStatuses.Add(readStatus);
        await _context.SaveChangesAsync();

        return Ok();
    }
}