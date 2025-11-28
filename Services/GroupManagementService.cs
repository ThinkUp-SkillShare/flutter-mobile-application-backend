using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;

namespace SkillShareBackend.Services;

/// <summary>
///     Service responsible for managing study group operations such as
///     permissions, member management, and statistics.
/// </summary>
public class GroupManagementService : IGroupManagementService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GroupManagementService> _logger;

    public GroupManagementService(AppDbContext context, ILogger<GroupManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Ownership Transfer

    /// <summary>
    ///     Transfers ownership of a group to another member.
    ///     Ensures new owner is a member and updates role to admin.
    /// </summary>
    public async Task<bool> TransferOwnership(int groupId, int newOwnerId, int currentOwnerId)
    {
        try
        {
            if (!await IsGroupOwner(groupId, currentOwnerId))
            {
                _logger.LogWarning($"User {currentOwnerId} is not the owner");
                return false;
            }

            var newOwnerMembership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == newOwnerId);

            if (newOwnerMembership == null)
            {
                _logger.LogWarning($"User {newOwnerId} is not a member of the group");
                return false;
            }

            var group = await _context.StudyGroups.FindAsync(groupId);
            if (group == null) return false;

            group.CreatedBy = newOwnerId;
            newOwnerMembership.Role = "admin";

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Ownership of group {groupId} transferred to user {newOwnerId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error transferring ownership of group {groupId}");
            return false;
        }
    }

    #endregion

    #region Statistics

    /// <summary>
    ///     Retrieves statistics for a specific group including member counts,
    ///     total messages, and last activity.
    /// </summary>
    public async Task<GroupStatisticsDto> GetGroupStatistics(int groupId)
    {
        var group = await _context.StudyGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return new GroupStatisticsDto();

        var adminCount = group.Members.Count(m => m.Role == "admin");
        var memberCount = group.Members.Count(m => m.Role == "member");

        var lastMessage = await _context.GroupMessages
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        var totalMessages = await _context.GroupMessages
            .CountAsync(m => m.GroupId == groupId);

        return new GroupStatisticsDto
        {
            TotalMembers = group.Members.Count,
            AdminCount = adminCount,
            MemberCount = memberCount,
            CreatedAt = group.CreatedAt,
            TotalMessages = totalMessages,
            LastActivity = lastMessage?.CreatedAt
        };
    }

    #endregion

    #region Permission Checks

    /// <summary>
    ///     Checks if a user is an admin of a specific group.
    /// </summary>
    public async Task<bool> IsGroupAdmin(int groupId, int userId)
    {
        var membership = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        return membership?.Role == "admin";
    }

    /// <summary>
    ///     Checks if a user is the owner (creator) of a specific group.
    /// </summary>
    public async Task<bool> IsGroupOwner(int groupId, int userId)
    {
        var group = await _context.StudyGroups.FindAsync(groupId);
        return group?.CreatedBy == userId;
    }

    /// <summary>
    ///     Checks if a user can manage members (either owner or admin).
    /// </summary>
    public async Task<bool> CanManageMembers(int groupId, int userId)
    {
        return await IsGroupAdmin(groupId, userId) || await IsGroupOwner(groupId, userId);
    }

    /// <summary>
    ///     Retrieves all permissions of a user for a specific group.
    /// </summary>
    public async Task<GroupPermissionsDto> GetUserPermissions(int groupId, int userId)
    {
        var isOwner = await IsGroupOwner(groupId, userId);
        var isAdmin = await IsGroupAdmin(groupId, userId);
        var membership = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        return new GroupPermissionsDto
        {
            IsOwner = isOwner,
            IsAdmin = isAdmin,
            IsMember = membership,
            CanEditGroup = isOwner || isAdmin,
            CanDeleteGroup = isOwner,
            CanManageMembers = isOwner || isAdmin,
            CanPromoteMembers = isOwner,
            CanRemoveMembers = isOwner || isAdmin,
            CanTransferOwnership = isOwner
        };
    }

    public async Task<bool> CanUserEditGroup(int groupId, int userId)
    {
        return await IsGroupAdmin(groupId, userId) || await IsGroupOwner(groupId, userId);
    }

    public async Task<bool> CanUserDeleteGroup(int groupId, int userId)
    {
        return await IsGroupOwner(groupId, userId);
    }

    #endregion

    #region Member Management

    /// <summary>
    ///     Promotes a member to admin. Only the owner can perform this action.
    /// </summary>
    public async Task<bool> PromoteToAdmin(int groupId, int targetUserId, int requestingUserId)
    {
        try
        {
            if (!await IsGroupOwner(groupId, requestingUserId))
            {
                _logger.LogWarning($"User {requestingUserId} attempted to promote without ownership");
                return false;
            }

            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == targetUserId);

            if (membership == null)
            {
                _logger.LogWarning($"User {targetUserId} is not a member of group {groupId}");
                return false;
            }

            if (membership.Role == "admin")
            {
                _logger.LogInformation($"User {targetUserId} is already an admin");
                return true; // Already an admin
            }

            membership.Role = "admin";
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {targetUserId} promoted to admin in group {groupId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error promoting user {targetUserId} to admin");
            return false;
        }
    }

    /// <summary>
    ///     Demotes an admin to member. Only the owner can perform this action.
    ///     Prevents demoting the only admin.
    /// </summary>
    public async Task<bool> DemoteToMember(int groupId, int targetUserId, int requestingUserId)
    {
        try
        {
            if (!await IsGroupOwner(groupId, requestingUserId))
            {
                _logger.LogWarning($"User {requestingUserId} attempted to demote without ownership");
                return false;
            }

            if (targetUserId == requestingUserId)
            {
                var adminCount = await _context.GroupMembers
                    .CountAsync(gm => gm.GroupId == groupId && gm.Role == "admin");

                if (adminCount <= 1)
                {
                    _logger.LogWarning("Cannot demote the only admin");
                    return false;
                }
            }

            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == targetUserId);

            if (membership == null) return false;

            membership.Role = "member";
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {targetUserId} demoted to member in group {groupId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error demoting user {targetUserId}");
            return false;
        }
    }

    /// <summary>
    ///     Removes a member from a group. Validates permissions and ownership rules.
    /// </summary>
    public async Task<bool> RemoveMember(int groupId, int targetUserId, int requestingUserId)
    {
        try
        {
            if (!await CanManageMembers(groupId, requestingUserId))
            {
                _logger.LogWarning($"User {requestingUserId} lacks permission to remove members");
                return false;
            }

            if (await IsGroupOwner(groupId, targetUserId))
            {
                _logger.LogWarning("Cannot remove the group owner");
                return false;
            }

            var isRequestingUserOwner = await IsGroupOwner(groupId, requestingUserId);
            var isTargetAdmin = await IsGroupAdmin(groupId, targetUserId);

            if (isTargetAdmin && !isRequestingUserOwner)
            {
                _logger.LogWarning("Only owner can remove admins");
                return false;
            }

            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == targetUserId);

            if (membership == null) return false;

            _context.GroupMembers.Remove(membership);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {targetUserId} removed from group {groupId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error removing user {targetUserId}");
            return false;
        }
    }

    /// <summary>
    ///     Removes multiple members from a group.
    /// </summary>
    public async Task<List<int>> BulkRemoveMembers(int groupId, List<int> userIds, int requestingUserId)
    {
        var removedUserIds = new List<int>();

        foreach (var userId in userIds)
            if (await RemoveMember(groupId, userId, requestingUserId))
                removedUserIds.Add(userId);

        return removedUserIds;
    }

    #endregion
}