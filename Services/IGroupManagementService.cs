using SkillShareBackend.DTOs;

namespace SkillShareBackend.Services;

/// <summary>
///     Defines all operations related to managing study groups,
///     including permissions, member management, ownership transfer, and statistics.
/// </summary>
public interface IGroupManagementService
{
    #region Ownership Management

    /// <summary>
    ///     Transfers ownership of a group to another member.
    ///     Ensures the new owner is a member and updates role to admin.
    /// </summary>
    Task<bool> TransferOwnership(int groupId, int newOwnerId, int currentOwnerId);

    #endregion

    #region Statistics

    /// <summary>
    ///     Retrieves statistics for a specific group including total members,
    ///     admin/member counts, total messages, and last activity.
    /// </summary>
    Task<GroupStatisticsDto> GetGroupStatistics(int groupId);

    #endregion

    #region Permission Checks

    /// <summary>
    ///     Checks if a given user is an admin of a specific group.
    /// </summary>
    Task<bool> IsGroupAdmin(int groupId, int userId);

    /// <summary>
    ///     Checks if a given user is the owner (creator) of a specific group.
    /// </summary>
    Task<bool> IsGroupOwner(int groupId, int userId);

    /// <summary>
    ///     Determines if a user can manage members of a specific group.
    ///     Typically true for admins or owners.
    /// </summary>
    Task<bool> CanManageMembers(int groupId, int userId);

    /// <summary>
    ///     Retrieves all permissions for a specific user in a group.
    /// </summary>
    Task<GroupPermissionsDto> GetUserPermissions(int groupId, int userId);

    /// <summary>
    ///     Checks if a user has permission to edit the group.
    /// </summary>
    Task<bool> CanUserEditGroup(int groupId, int userId);

    /// <summary>
    ///     Checks if a user has permission to delete the group.
    /// </summary>
    Task<bool> CanUserDeleteGroup(int groupId, int userId);

    #endregion

    #region Member Management

    /// <summary>
    ///     Promotes a member to admin. Only the owner can perform this action.
    /// </summary>
    Task<bool> PromoteToAdmin(int groupId, int targetUserId, int requestingUserId);

    /// <summary>
    ///     Demotes an admin to a regular member. Only the owner can perform this action.
    /// </summary>
    Task<bool> DemoteToMember(int groupId, int targetUserId, int requestingUserId);

    /// <summary>
    ///     Removes a member from the group. Validates permissions and ownership rules.
    /// </summary>
    Task<bool> RemoveMember(int groupId, int targetUserId, int requestingUserId);

    /// <summary>
    ///     Removes multiple members from a group. Returns a list of successfully removed user IDs.
    /// </summary>
    Task<List<int>> BulkRemoveMembers(int groupId, List<int> userIds, int requestingUserId);

    #endregion
}