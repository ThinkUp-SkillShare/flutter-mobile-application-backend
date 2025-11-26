namespace SkillShareBackend.DTOs
{
    /// <summary>
    /// Represents a complete view of a study group returned to the client.
    /// Includes metadata, subject, creator information, and user-specific role.
    /// </summary>
    public class StudyGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public int CreatedBy { get; set; }
        public string? CreatorName { get; set; }
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }

        /// <summary>
        /// The role of the requesting user inside the group ("admin", "member", or null if not a member).
        /// </summary>
        public string? UserRole { get; set; }
    }

    /// <summary>
    /// Data required to create a new study group.
    /// </summary>
    public class CreateStudyGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }

        /// <summary>
        /// Optional subject associated with the group.
        /// </summary>
        public int? SubjectId { get; set; }
    }

    /// <summary>
    /// Fields that can be updated in an existing study group.
    /// All properties are optional.
    /// </summary>
    public class UpdateStudyGroupDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public int? SubjectId { get; set; }
    }

    /// <summary>
    /// DTO used when a user requests to join a group.
    /// </summary>
    public class JoinGroupDto
    {
        public int GroupId { get; set; }
    }

    /// <summary>
    /// Represents a member inside a study group,
    /// including user details, role, and join date.
    /// </summary>
    public class GroupMemberDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string? UserName { get; set; }

        /// <summary>
        /// Role assigned to the user inside the group ("admin" or "member").
        /// </summary>
        public string Role { get; set; } = "member";

        /// <summary>
        /// Date the user joined the group (if tracked).
        /// </summary>
        public DateTime? JoinedAt { get; set; }
    }

    /// <summary>
    /// Represents a subject that groups can be categorized under.
    /// </summary>
    public class SubjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Used to update a member’s role inside a study group.
    /// </summary>
    public class UpdateMemberRoleDto
    {
        public int UserId { get; set; }
        public string NewRole { get; set; } = "member";
    }

    /// <summary>
    /// Simple DTO used to remove a member from a group.
    /// </summary>
    public class RemoveMemberDto
    {
        public int UserId { get; set; }
    }

    /// <summary>
    /// Used to transfer the ownership of a group to another member.
    /// </summary>
    public class TransferOwnershipDto
    {
        public int NewOwnerId { get; set; }
    }

    /// <summary>
    /// Represents all permissions a user has inside a specific group.
    /// Used for access control on the frontend.
    /// </summary>
    public class GroupPermissionsDto
    {
        public bool CanEditGroup { get; set; }
        public bool CanDeleteGroup { get; set; }
        public bool CanManageMembers { get; set; }
        public bool CanPromoteMembers { get; set; }
        public bool CanRemoveMembers { get; set; }
        public bool CanTransferOwnership { get; set; }

        /// <summary>
        /// Indicates whether the user holds ownership or admin privileges.
        /// </summary>
        public bool IsOwner { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsMember { get; set; }
    }

    /// <summary>
    /// Statistics and historical information about a group.
    /// </summary>
    public class GroupStatisticsDto
    {
        public int TotalMembers { get; set; }
        public int AdminCount { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Aggregated number of messages sent in group activities.
        /// </summary>
        public int TotalMessages { get; set; }

        /// <summary>
        /// Timestamp of the most recent interaction/activity.
        /// </summary>
        public DateTime? LastActivity { get; set; }
    }

    /// <summary>
    /// Used to remove several users from a group at once.
    /// </summary>
    public class BulkRemoveMembersDto
    {
        public List<int> UserIds { get; set; } = new();
    }

    /// <summary>
    /// Used to invite a member to the group by their email.
    /// </summary>
    public class InviteMemberDto
    {
        public string Email { get; set; } = string.Empty;
    }
}
