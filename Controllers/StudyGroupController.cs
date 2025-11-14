using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;
using System.Security.Claims;

namespace SkillShareBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudyGroupController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StudyGroupController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudyGroupDto>>> GetAllGroups()
        {
            try
            {
                Console.WriteLine("🔍 DEBUG: GetAllGroups endpoint called");
        
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

                Console.WriteLine($"✅ DEBUG: Returning {groups.Count} groups");
                return Ok(groups);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DEBUG: Error in GetAllGroups: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        
        // GET: api/StudyGroup/user/{userId}
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

        // GET: api/StudyGroup/recent?userId={userId}
        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<StudyGroupDto>>> GetRecentGroups([FromQuery] int userId)
        {
            // Obtener grupos donde el usuario NO es miembro
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

        // GET: api/StudyGroup/{id}
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
        
        // GET: api/StudyGroup/featured?userId={userId}
        [HttpGet("featured")]
        public async Task<ActionResult<IEnumerable<StudyGroupDto>>> GetFeaturedGroups([FromQuery] int userId)
        {
            Console.WriteLine($"🔍 DEBUG: GetFeaturedGroups called for userId {userId}");

            // Obtener IDs de grupos donde el usuario ya es miembro
            var userGroupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            Console.WriteLine($"📊 DEBUG: User is member of {userGroupIds.Count} groups");

            // Obtener grupos donde el usuario NO es miembro
            var featuredGroups = await _context.StudyGroups
                .Where(g => !userGroupIds.Contains(g.Id))
                .Include(g => g.Subject)
                .Include(g => g.Creator)
                .Include(g => g.Members)
                .OrderByDescending(g => g.Members.Count)
                .ThenByDescending(g => g.CreatedAt)
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

            Console.WriteLine($"✅ DEBUG: Returning {featuredGroups.Count} featured groups");

            return Ok(featuredGroups);
        }

        // POST: api/StudyGroup
        [HttpPost]
        public async Task<ActionResult<StudyGroupDto>> CreateStudyGroup([FromBody] CreateStudyGroupDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var group = new StudyGroup
            {
                Name = dto.Name,
                Description = dto.Description,
                CoverImage = dto.CoverImage,
                CreatedBy = userId,
                SubjectId = dto.SubjectId,
                CreatedAt = DateTime.UtcNow
            };

            _context.StudyGroups.Add(group);
            await _context.SaveChangesAsync();

            // Agregar al creador como admin del grupo
            var membership = new GroupMember
            {
                GroupId = group.Id,
                UserId = userId,
                Role = "admin"
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

        // PUT: api/StudyGroup/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudyGroup(int id, [FromBody] UpdateStudyGroupDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var group = await _context.StudyGroups.FindAsync(id);
            if (group == null)
            {
                return NotFound(new { message = "Group not found" });
            }

            // Verificar que el usuario sea admin del grupo
            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == id && gm.UserId == userId);

            if (membership?.Role != "admin" && group.CreatedBy != userId)
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

        // DELETE: api/StudyGroup/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudyGroup(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var group = await _context.StudyGroups.FindAsync(id);
            if (group == null)
            {
                return NotFound(new { message = "Group not found" });
            }

            if (group.CreatedBy != userId)
            {
                return Forbid();
            }

            _context.StudyGroups.Remove(group);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/StudyGroup/{id}/join
        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinGroup(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var group = await _context.StudyGroups.FindAsync(id);
            if (group == null)
            {
                return NotFound(new { message = "Group not found" });
            }

            // Verificar si ya es miembro
            var existingMembership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == id && gm.UserId == userId);

            if (existingMembership != null)
            {
                return BadRequest(new { message = "Already a member of this group" });
            }

            var membership = new GroupMember
            {
                GroupId = id,
                UserId = userId,
                Role = "member"
            };

            _context.GroupMembers.Add(membership);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Successfully joined the group" });
        }

        // DELETE: api/StudyGroup/{id}/leave
        [HttpDelete("{id}/leave")]
        public async Task<IActionResult> LeaveGroup(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == id && gm.UserId == userId);

            if (membership == null)
            {
                return NotFound(new { message = "Membership not found" });
            }

            // No permitir que el creador salga si es el único admin
            var group = await _context.StudyGroups.FindAsync(id);
            if (group != null && group.CreatedBy == userId)
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

        // GET: api/StudyGroup/{id}/members
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
        
        
    }
}