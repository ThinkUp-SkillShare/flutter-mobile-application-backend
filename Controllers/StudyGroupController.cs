using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;
using SkillShareBackend.Services;
using System.Security.Claims;

namespace SkillShareBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Ensures all endpoints require authentication
    public class StudyGroupController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IGroupManagementService _groupManagementService;

        public StudyGroupController(AppDbContext context, IGroupManagementService groupManagementService)
        {
            _context = context;
            _groupManagementService = groupManagementService;
        }

        /// <summary>
        /// Retrieves authenticated user's ID from JWT token.
        /// </summary>
        private int? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }

        #region CRUD Básico (ya existente)
        // Basic CRUD operations for study groups

        /// <summary>
        /// Returns all study groups (public list).
        /// Includes subject, creator, and member count.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudyGroupDto>>> GetAllGroups()
        {
            try
            {
                var groups = await _context.StudyGroups
                    .Include(g => g.Subject)
                    .Include(g => g.Creator)
                    .Include(g => g.Members)
                    .OrderByDescending(g => g.CreatedAt)
                    .Select(g => new StudyGroupDto
                    {
                        Id = g.Id,
                        Name = g.Name,
                        Description = g.Description,
                        CoverImage = g.CoverImage,
                        CreatedBy = g.CreatedBy,
                        CreatorName = g.Creator != null ? g.Creator.Email : null,
                        SubjectId = g.SubjectId,
                        SubjectName = g.Subject != null ? g.Subject.Name : null,
                        CreatedAt = g.CreatedAt,
                        MemberCount = g.Members.Count,
                        UserRole = null
                    })
                    .ToListAsync();

                return Ok(groups);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Returns all groups where the specified user is a member.
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<StudyGroupDto>>> GetUserGroups(int userId)
        {
            var groups = await _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Include(gm => gm.Group) 
                    .ThenInclude(g => g!.Subject)
                .Include(gm => gm.Group) 
                    .ThenInclude(g => g!.Creator)
                .Select(gm => new StudyGroupDto
                {
                    Id = gm.Group!.Id,
                    Name = gm.Group.Name,
                    Description = gm.Group.Description,
                    CoverImage = gm.Group.CoverImage,
                    CreatedBy = gm.Group.CreatedBy,
                    CreatorName = gm.Group.Creator != null ? gm.Group.Creator.Email : null,
                    SubjectId = gm.Group.SubjectId,
                    SubjectName = gm.Group.Subject != null ? gm.Group.Subject.Name : null,
                    CreatedAt = gm.Group.CreatedAt,
                    MemberCount = gm.Group.Members.Count,
                    UserRole = gm.Role
                })
                .ToListAsync();

            return Ok(groups);
        }

        /// <summary>
        /// Returns a single study group by ID, optionally including the user's role in the group.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<StudyGroupDto>> GetStudyGroup(int id, [FromQuery] int? userId = null)
        {
            var group = await _context.StudyGroups
                .Include(g => g.Subject)
                .Include(g => g.Creator)
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound(new { message = "Group not found" });
            }

            string? userRole = null;
            if (userId.HasValue)
            {
                var membership = await _context.GroupMembers
                    .FirstOrDefaultAsync(gm => gm.GroupId == id && gm.UserId == userId.Value);
                userRole = membership?.Role;
            }

            var dto = new StudyGroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CoverImage = group.CoverImage,
                CreatedBy = group.CreatedBy,
                CreatorName = group.Creator?.Email,
                SubjectId = group.SubjectId,
                SubjectName = group.Subject?.Name,
                CreatedAt = group.CreatedAt,
                MemberCount = group.Members.Count,
                UserRole = userRole
            };

            return Ok(dto);
        }

        /// <summary>
        /// Creates a new study group and assigns the creator as admin.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<StudyGroupDto>> CreateStudyGroup([FromBody] CreateStudyGroupDto dto)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var group = new StudyGroup
            {
                Name = dto.Name,
                Description = dto.Description,
                CoverImage = dto.CoverImage,
                CreatedBy = userId.Value,
                SubjectId = dto.SubjectId,
                CreatedAt = DateTime.UtcNow
            };

            _context.StudyGroups.Add(group);
            await _context.SaveChangesAsync();

            var membership = new GroupMember
            {
                GroupId = group.Id,
                UserId = userId.Value,
                Role = "admin" // creator is automatically admin
            };

            _context.GroupMembers.Add(membership);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudyGroup), new { id = group.Id }, new StudyGroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CoverImage = group.CoverImage,
                CreatedBy = group.CreatedBy,
                SubjectId = group.SubjectId,
                CreatedAt = group.CreatedAt,
                MemberCount = 1,
                UserRole = "admin"
            });
        }

        /// <summary>
        /// Updates a study group. User must have edit permissions.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudyGroup(int id, [FromBody] UpdateStudyGroupDto dto)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var group = await _context.StudyGroups.FindAsync(id);
            if (group == null)
            {
                return NotFound(new { message = "Group not found" });
            }

            // Permissions handled by service
            if (!await _groupManagementService.CanUserEditGroup(id, userId.Value))
            {
                return Forbid();
            }

            if (!string.IsNullOrEmpty(dto.Name))
                group.Name = dto.Name;
            if (dto.Description != null)
                group.Description = dto.Description;
            if (dto.CoverImage != null)
                group.CoverImage = dto.CoverImage;
            if (dto.SubjectId.HasValue)
                group.SubjectId = dto.SubjectId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Deletes a study group. Only owner/admin may delete.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudyGroup(int id)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var group = await _context.StudyGroups.FindAsync(id);
            if (group == null)
            {
                return NotFound(new { message = "Group not found" });
            }

            // Permission validated through service
            if (!await _groupManagementService.CanUserDeleteGroup(id, userId.Value))
            {
                return Forbid();
            }

            _context.StudyGroups.Remove(group);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion

        #region Membership
        // Join, leave and list members logic

        /// <summary>
        /// Allows a user to join a study group.
        /// </summary>
        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinGroup(int id)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var group = await _context.StudyGroups.FindAsync(id);
            if (group == null)
            {
                return NotFound(new { message = "Group not found" });
            }

            var existingMembership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == id && gm.UserId == userId.Value);

            if (existingMembership != null)
            {
                return BadRequest(new { message = "Already a member of this group" });
            }

            var membership = new GroupMember
            {
                GroupId = id,
                UserId = userId.Value,
                Role = "member"
            };

            _context.GroupMembers.Add(membership);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Successfully joined the group" });
        }

        /// <summary>
        /// Allows a user to leave a study group.
        /// Prevents the last admin from leaving.
        /// </summary>
        [HttpDelete("{id}/leave")]
        public async Task<IActionResult> LeaveGroup(int id)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == id && gm.UserId == userId.Value);

            if (membership == null)
            {
                return NotFound(new { message = "Membership not found" });
            }

            var group = await _context.StudyGroups.FindAsync(id);
            if (group != null && group.CreatedBy == userId.Value)
            {
                var adminCount = await _context.GroupMembers
                    .CountAsync(gm => gm.GroupId == id && gm.Role == "admin");

                if (adminCount <= 1)
                {
                    return BadRequest(new { message = "Cannot leave: you are the only admin" });
                }
            }

            _context.GroupMembers.Remove(membership);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Successfully left the group" });
        }

        /// <summary>
        /// Returns all members of a study group.
        /// </summary>
        [HttpGet("{id}/members")]
        public async Task<ActionResult<IEnumerable<GroupMemberDto>>> GetGroupMembers(int id)
        {
            var members = await _context.GroupMembers
                .Where(gm => gm.GroupId == id)
                .Include(gm => gm.User)
                .Select(gm => new GroupMemberDto
                {
                    Id = gm.Id,
                    GroupId = gm.GroupId,
                    UserId = gm.UserId,
                    UserEmail = gm.User!.Email,
                    Role = gm.Role
                })
                .ToListAsync();

            return Ok(members);
        }

        #endregion

        #region Admin Management

        /// <summary>
        /// Returns permissions for the logged-in user inside a specific group.
        /// </summary>
        [HttpGet("{id}/permissions")]
        public async Task<ActionResult<GroupPermissionsDto>> GetUserPermissions(int id)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var permissions = await _groupManagementService.GetUserPermissions(id, userId.Value);
            return Ok(permissions);
        }

        /// <summary>
        /// Returns statistics about a study group (e.g., member count, activity).
        /// Only accessible by members.
        /// </summary>
        [HttpGet("{id}/statistics")]
        public async Task<ActionResult<GroupStatisticsDto>> GetGroupStatistics(int id)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == id && gm.UserId == userId.Value);

            if (!isMember)
            {
                return Forbid();
            }

            var statistics = await _groupManagementService.GetGroupStatistics(id);
            return Ok(statistics);
        }

        /// <summary>
        /// Promotes a member to admin (requires admin privileges).
        /// </summary>
        [HttpPost("{id}/members/{memberId}/promote")]
        public async Task<IActionResult> PromoteToAdmin(int id, int memberId)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _groupManagementService.PromoteToAdmin(id, memberId, userId.Value);

            if (!result)
            {
                return BadRequest(new { message = "Failed to promote member. You may lack permissions or the member may already be an admin." });
            }

            return Ok(new { message = "Member promoted to admin successfully" });
        }

        /// <summary>
        /// Demotes an admin to regular member (cannot demote last admin).
        /// </summary>
        [HttpPost("{id}/members/{memberId}/demote")]
        public async Task<IActionResult> DemoteToMember(int id, int memberId)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _groupManagementService.DemoteToMember(id, memberId, userId.Value);

            if (!result)
            {
                return BadRequest(new { message = "Failed to demote admin. You may lack permissions or this is the only admin." });
            }

            return Ok(new { message = "Admin demoted to member successfully" });
        }

        /// <summary>
        /// Removes a member from the group (requires admin privileges).
        /// </summary>
        [HttpDelete("{id}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(int id, int memberId)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _groupManagementService.RemoveMember(id, memberId, userId.Value);

            if (!result)
            {
                return BadRequest(new { message = "Failed to remove member. You may lack permissions or cannot remove this user." });
            }

            return Ok(new { message = "Member removed successfully" });
        }

        /// <summary>
        /// Bulk-removes multiple users from a group.
        /// </summary>
        [HttpPost("{id}/members/bulk-remove")]
        public async Task<ActionResult<List<int>>> BulkRemoveMembers(int id, [FromBody] BulkRemoveMembersDto dto)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var removedIds = await _groupManagementService.BulkRemoveMembers(id, dto.UserIds, userId.Value);

            return Ok(new
            {
                message = $"Removed {removedIds.Count} of {dto.UserIds.Count} members",
                removedUserIds = removedIds
            });
        }

        /// <summary>
        /// Transfers group ownership to another existing member.
        /// </summary>
        [HttpPost("{id}/transfer-ownership")]
        public async Task<IActionResult> TransferOwnership(int id, [FromBody] TransferOwnershipDto dto)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _groupManagementService.TransferOwnership(id, dto.NewOwnerId, userId.Value);

            if (!result)
            {
                return BadRequest(new { message = "Failed to transfer ownership. You must be the owner and the target must be a member." });
            }

            return Ok(new { message = "Ownership transferred successfully" });
        }

        #endregion

        #region Featured & Recent
        // Group discovery endpoints (featured & recent)

        /// <summary>
        /// Returns most popular groups the user is not part of.
        /// </summary>
        [HttpGet("featured")]
        public async Task<ActionResult<IEnumerable<StudyGroupDto>>> GetFeaturedGroups([FromQuery] int userId)
        {
            var userGroupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            var featuredGroups = await _context.StudyGroups
                .Where(g => !userGroupIds.Contains(g.Id))
                .Include(g => g.Subject)
                .Include(g => g.Creator)
                .Include(g => g.Members)
                .Select(g => new
                {
                    Group = g,
                    MemberCount = g.Members.Count
                })
                .OrderByDescending(x => x.MemberCount)
                .ThenByDescending(x => x.Group.CreatedAt)
                .Take(10)
                .Select(x => new StudyGroupDto
                {
                    Id = x.Group.Id,
                    Name = x.Group.Name,
                    Description = x.Group.Description,
                    CoverImage = x.Group.CoverImage,
                    CreatedBy = x.Group.CreatedBy,
                    CreatorName = x.Group.Creator != null ? x.Group.Creator.Email : null,
                    SubjectId = x.Group.SubjectId,
                    SubjectName = x.Group.Subject != null ? x.Group.Subject.Name : null,
                    CreatedAt = x.Group.CreatedAt,
                    MemberCount = x.MemberCount,
                    UserRole = null
                })
                .ToListAsync();

            return Ok(featuredGroups);
        }

        /// <summary>
        /// Returns recently created groups the user is not part of.
        /// </summary>
        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<StudyGroupDto>>> GetRecentGroups([FromQuery] int userId)
        {
            var userGroupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            var recentGroups = await _context.StudyGroups
                .Where(g => !userGroupIds.Contains(g.Id))
                .Include(g => g.Subject)
                .Include(g => g.Creator)
                .Include(g => g.Members)
                .OrderByDescending(g => g.CreatedAt)
                .Take(10)
                .Select(g => new StudyGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    CoverImage = g.CoverImage,
                    CreatedBy = g.CreatedBy,
                    CreatorName = g.Creator != null ? g.Creator.Email : null,
                    SubjectId = g.SubjectId,
                    SubjectName = g.Subject != null ? g.Subject.Name : null,
                    CreatedAt = g.CreatedAt,
                    MemberCount = g.Members.Count,
                    UserRole = null
                })
                .ToListAsync();

            return Ok(recentGroups);
        }

        #endregion
    }
}
