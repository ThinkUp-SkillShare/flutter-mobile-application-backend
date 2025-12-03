using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;
using SkillShareBackend.Services;

namespace SkillShareBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IFirebaseStorageService _firebaseStorageService;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(
        AppDbContext context,
        ILogger<DocumentController> logger,
        IWebHostEnvironment environment,
        IFirebaseStorageService firebaseStorageService
    )
    {
        _context = context;
        _logger = logger;
        _environment = environment;
        _firebaseStorageService = firebaseStorageService;
    }

    /// <summary>
    ///     Gets the current user's ID from the JWT token
    /// </summary>
    // En el método GetUserId(), hay un problema con la extracción del UserId
    private int GetUserId()
    {
        try
        {
            // La respuesta del login muestra: "uid": "2"
            // Ver todos los claims disponibles
            var claims = User.Claims.ToList();
            _logger.LogInformation($"Available claims: {string.Join(", ", claims.Select(c => $"{c.Type}:{c.Value}"))}");

            // Priorizar "uid" que es lo que se envía en el token
            var userIdClaim = User.FindFirst("uid")?.Value
                              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("userId")?.Value
                              ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogError("No user ID claim found in token");
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            if (!int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogError($"Failed to parse user ID from claim: {userIdClaim}");
                throw new UnauthorizedAccessException($"Invalid user ID format: {userIdClaim}");
            }

            _logger.LogInformation($"Extracted user ID: {userId}");
            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user ID from token");
            throw;
        }
    }

    // GET: api/document/statistics/global
    [HttpGet("statistics/global")]
    public async Task<ActionResult<object>> GetGlobalStatistics()
    {
        try
        {
            var userId = GetUserId();

            var totalDocuments = await _context.GroupDocuments.CountAsync();
            var userDocuments = await _context.GroupDocuments.CountAsync(d => d.UserId == userId);
            var totalSize = await _context.GroupDocuments.SumAsync(d => d.FileSize ?? 0);

            return Ok(new
            {
                TotalDocuments = totalDocuments,
                MyDocuments = userDocuments,
                TotalSize = totalSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting global statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET: api/document/group/{groupId}
    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<IEnumerable<GroupDocumentDto>>> GetGroupDocuments(int groupId)
    {
        try
        {
            var userId = GetUserId();

            // Verify user is a member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (!isMember) return Forbid();

            var documents = await _context.GroupDocuments
                .Where(d => d.GroupId == groupId)
                .Include(d => d.User)
                .Include(d => d.Group)
                .Include(d => d.Subject)
                .OrderByDescending(d => d.UploadDate)
                .Select(d => new GroupDocumentDto
                {
                    Id = d.Id,
                    GroupId = d.GroupId,
                    UserId = d.UserId,
                    UserEmail = d.User!.Email,
                    Title = d.Title,
                    Description = d.Description,
                    FileName = d.FileName,
                    FileUrl = d.FileUrl,
                    FileSize = d.FileSize,
                    FileType = d.FileType,
                    SubjectId = d.SubjectId,
                    SubjectName = d.Subject != null ? d.Subject.Name : "General",
                    UploadDate = d.UploadDate,
                    DownloadCount = d.DownloadCount,
                    FavoriteCount = d.FavoriteCount ?? 0,
                    IsFavorite = _context.DocumentFavorites
                        .Any(df => df.DocumentId == d.Id && df.UserId == userId)
                })
                .ToListAsync();

            return Ok(documents);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents for group {GroupId}", groupId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET: api/document/user
    [HttpGet("user")]
    public async Task<ActionResult<IEnumerable<GroupDocumentDto>>> GetUserDocuments()
    {
        try
        {
            _logger.LogInformation("📥 GET /api/document/user called");

            var userId = GetUserId();
            _logger.LogInformation($"🔑 User ID: {userId}");

            var documents = await _context.GroupDocuments
                .Where(d => d.UserId == userId)
                .Include(d => d.User)
                .Include(d => d.Group)
                .Include(d => d.Subject)
                .OrderByDescending(d => d.UploadDate)
                .Select(d => new GroupDocumentDto
                {
                    Id = d.Id,
                    GroupId = d.GroupId,
                    GroupName = d.Group!.Name,
                    UserId = d.UserId,
                    UserEmail = d.User!.Email,
                    Title = d.Title,
                    Description = d.Description,
                    FileName = d.FileName,
                    FileUrl = d.FileUrl,
                    FileSize = d.FileSize,
                    FileType = d.FileType,
                    SubjectId = d.SubjectId,
                    SubjectName = d.Subject != null ? d.Subject.Name : "General",
                    UploadDate = d.UploadDate,
                    DownloadCount = d.DownloadCount,
                    FavoriteCount = d.FavoriteCount ?? 0,
                    IsFavorite = _context.DocumentFavorites
                        .Any(df => df.DocumentId == d.Id && df.UserId == userId)
                })
                .ToListAsync();

            _logger.LogInformation($"📄 Found {documents.Count} documents for user {userId}");
            return Ok(documents);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "❌ Unauthorized access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting user documents");
            return StatusCode(500, new
            {
                message = "Internal server error",
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    // GET: api/document/favorites
    [HttpGet("favorites")]
    public async Task<ActionResult<IEnumerable<GroupDocumentDto>>> GetFavoriteDocuments()
    {
        try
        {
            var userId = GetUserId();

            var favoriteDocuments = await _context.DocumentFavorites
                .Where(df => df.UserId == userId)
                .Include(df => df.Document)
                .ThenInclude(d => d.User)
                .Include(df => df.Document)
                .ThenInclude(d => d.Group)
                .Include(df => df.Document)
                .ThenInclude(d => d.Subject)
                .OrderByDescending(df => df.CreatedAt)
                .Select(df => new GroupDocumentDto
                {
                    Id = df.Document.Id,
                    GroupId = df.Document.GroupId,
                    GroupName = df.Document.Group!.Name,
                    UserId = df.Document.UserId,
                    UserEmail = df.Document.User!.Email,
                    Title = df.Document.Title,
                    Description = df.Document.Description,
                    FileName = df.Document.FileName,
                    FileUrl = df.Document.FileUrl,
                    FileSize = df.Document.FileSize,
                    FileType = df.Document.FileType,
                    SubjectId = df.Document.SubjectId,
                    SubjectName = df.Document.Subject != null ? df.Document.Subject.Name : "General",
                    UploadDate = df.Document.UploadDate,
                    DownloadCount = df.Document.DownloadCount,
                    FavoriteCount = df.Document.FavoriteCount ?? 0,
                    IsFavorite = true
                })
                .ToListAsync();

            return Ok(favoriteDocuments);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite documents");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET: api/document/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<GroupDocumentDto>> GetDocument(int id)
    {
        try
        {
            var userId = GetUserId();

            var document = await _context.GroupDocuments
                .Include(d => d.User)
                .Include(d => d.Group)
                .Include(d => d.Subject)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null) return NotFound(new { message = "Document not found" });

            // Verify user is a member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == document.GroupId && gm.UserId == userId);

            if (!isMember) return Forbid();

            var isFavorite = await _context.DocumentFavorites
                .AnyAsync(df => df.DocumentId == id && df.UserId == userId);

            var dto = new GroupDocumentDto
            {
                Id = document.Id,
                GroupId = document.GroupId,
                GroupName = document.Group?.Name,
                UserId = document.UserId,
                UserEmail = document.User!.Email,
                Title = document.Title,
                Description = document.Description,
                FileName = document.FileName,
                FileUrl = document.FileUrl,
                FileSize = document.FileSize,
                FileType = document.FileType,
                SubjectId = document.SubjectId,
                SubjectName = document.Subject?.Name,
                UploadDate = document.UploadDate,
                DownloadCount = document.DownloadCount,
                FavoriteCount = document.FavoriteCount ?? 0,
                IsFavorite = isFavorite
            };

            return Ok(dto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // POST: api/document/upload
    [HttpPost("upload")]
    public async Task<ActionResult<GroupDocumentDto>> UploadDocument([FromForm] UploadDocumentDto dto)
    {
        try
        {
            var userId = GetUserId();

            // Verificar que el usuario es miembro del grupo
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == dto.GroupId && gm.UserId == userId);

            if (!isMember) return Forbid();

            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            // Validar tamaño máximo (50MB)
            if (dto.File.Length > 50 * 1024 * 1024)
                return BadRequest(new { message = "File size exceeds 50MB limit" });

            // Subir archivo a Firebase Storage
            var fileUrl = await _firebaseStorageService.UploadFileAsync(dto.File);
            var fileType = _firebaseStorageService.GetFileType(dto.File.FileName);

            // Verificar si el subject existe
            if (dto.SubjectId.HasValue)
            {
                var subjectExists = await _context.Subjects
                    .AnyAsync(s => s.Id == dto.SubjectId.Value);

                if (!subjectExists)
                    return BadRequest(new { message = "Invalid subject ID" });
            }

            // Crear documento en la base de datos
            var document = new GroupDocument
            {
                GroupId = dto.GroupId,
                UserId = userId,
                Title = dto.Title,
                Description = dto.Description,
                FileName = dto.File.FileName,
                FileUrl = fileUrl, // URL de Firebase Storage
                FileSize = dto.File.Length,
                FileType = fileType,
                SubjectId = dto.SubjectId,
                UploadDate = DateTime.UtcNow,
                DownloadCount = 0,
                FavoriteCount = 0
            };

            _context.GroupDocuments.Add(document);
            await _context.SaveChangesAsync();

            // Cargar relaciones para la respuesta
            await _context.Entry(document)
                .Reference(d => d.Subject)
                .LoadAsync();
            await _context.Entry(document)
                .Reference(d => d.User)
                .LoadAsync();
            await _context.Entry(document)
                .Reference(d => d.Group)
                .LoadAsync();

            var resultDto = new GroupDocumentDto
            {
                Id = document.Id,
                GroupId = document.GroupId,
                GroupName = document.Group?.Name,
                UserId = document.UserId,
                UserEmail = document.User?.Email ?? string.Empty,
                Title = document.Title,
                Description = document.Description,
                FileName = document.FileName,
                FileUrl = document.FileUrl,
                FileSize = document.FileSize,
                FileType = document.FileType,
                SubjectId = document.SubjectId,
                SubjectName = document.Subject?.Name,
                UploadDate = document.UploadDate,
                DownloadCount = document.DownloadCount,
                FavoriteCount = document.FavoriteCount ?? 0,
                IsFavorite = false
            };

            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // POST: api/document/{id}/download
    [HttpPost("{id}/download")]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        try
        {
            var userId = GetUserId();

            var document = await _context.GroupDocuments.FindAsync(id);

            if (document == null)
                return NotFound(new { message = "Document not found" });

            // Verify user is a member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == document.GroupId && gm.UserId == userId);

            if (!isMember) return Forbid();

            // Increment download counter
            document.DownloadCount++;
            await _context.SaveChangesAsync();

            // Descargar archivo de Firebase Storage
            var fileBytes = await _firebaseStorageService.DownloadFileAsync(document.FileUrl);

            return File(fileBytes, "application/octet-stream", document.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // DELETE: api/document/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        try
        {
            var userId = GetUserId();

            var document = await _context.GroupDocuments.FindAsync(id);

            if (document == null)
                return NotFound(new { message = "Document not found" });

            // Solo el propietario puede eliminar
            if (document.UserId != userId)
                return Forbid();

            // Eliminar archivo de Firebase Storage
            await _firebaseStorageService.DeleteFileAsync(document.FileUrl);

            // Eliminar todos los favoritos asociados
            var favorites = await _context.DocumentFavorites
                .Where(df => df.DocumentId == id)
                .ToListAsync();

            _context.DocumentFavorites.RemoveRange(favorites);
            _context.GroupDocuments.Remove(document);

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT: api/document/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(int id, [FromBody] UpdateDocumentDto dto)
    {
        try
        {
            var userId = GetUserId();

            var document = await _context.GroupDocuments
                .Include(d => d.Subject)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
                return NotFound(new { message = "Document not found" });

            // Only owner can edit
            if (document.UserId != userId)
                return Forbid();

            if (!string.IsNullOrEmpty(dto.Title))
                document.Title = dto.Title;

            if (dto.Description != null)
                document.Description = dto.Description;

            if (dto.SubjectId.HasValue)
            {
                // Verify subject exists
                var subjectExists = await _context.Subjects
                    .AnyAsync(s => s.Id == dto.SubjectId.Value);

                if (!subjectExists)
                    return BadRequest(new { message = "Invalid subject ID" });

                document.SubjectId = dto.SubjectId.Value;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // POST: api/document/{id}/favorite
    [HttpPost("{id}/favorite")]
    public async Task<ActionResult<object>> ToggleFavorite(int id)
    {
        try
        {
            var userId = GetUserId();

            var document = await _context.GroupDocuments.FindAsync(id);

            if (document == null)
                return NotFound(new { message = "Document not found" });

            // Verify user is a member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == document.GroupId && gm.UserId == userId);

            if (!isMember)
                return Forbid();

            // Check if already favorited
            var existingFavorite = await _context.DocumentFavorites
                .FirstOrDefaultAsync(df => df.DocumentId == id && df.UserId == userId);

            if (existingFavorite != null)
            {
                // Remove from favorites
                _context.DocumentFavorites.Remove(existingFavorite);
                document.FavoriteCount = Math.Max(0, (document.FavoriteCount ?? 1) - 1);
            }
            else
            {
                // Add to favorites
                var favorite = new DocumentFavorite
                {
                    DocumentId = id,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentFavorites.Add(favorite);
                document.FavoriteCount = (document.FavoriteCount ?? 0) + 1;
            }

            await _context.SaveChangesAsync();

            // Check if now favorited
            var isFavorite = await _context.DocumentFavorites
                .AnyAsync(df => df.DocumentId == id && df.UserId == userId);

            return Ok(new
            {
                isFavorite,
                favoriteCount = document.FavoriteCount
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite for document {DocumentId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET: api/document/statistics/group/{groupId}
    [HttpGet("statistics/group/{groupId}")]
    public async Task<ActionResult<DocumentStatisticsDto>> GetGroupStatistics(int groupId)
    {
        try
        {
            var userId = GetUserId();

            // Verify user is a member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (!isMember)
                return Forbid();

            var statistics = await _context.GroupDocuments
                .Where(d => d.GroupId == groupId)
                .GroupBy(d => 1)
                .Select(g => new DocumentStatisticsDto
                {
                    TotalDocuments = g.Count(),
                    TotalSize = g.Sum(d => d.FileSize ?? 0),
                    PdfCount = g.Count(d => d.FileType == "pdf"),
                    DocumentCount = g.Count(d => d.FileType == "document"),
                    PresentationCount = g.Count(d => d.FileType == "presentation"),
                    SpreadsheetCount = g.Count(d => d.FileType == "spreadsheet"),
                    ImageCount = g.Count(d => d.FileType == "image"),
                    OtherCount = g.Count(d => d.FileType == "other")
                })
                .FirstOrDefaultAsync() ?? new DocumentStatisticsDto();

            return Ok(statistics);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document statistics for group {GroupId}", groupId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET: api/document/subjects/popular
    [HttpGet("subjects/popular")]
    public async Task<ActionResult<IEnumerable<object>>> GetPopularSubjectsForDocuments()
    {
        var subjects = await _context.GroupDocuments
            .Where(d => d.SubjectId != null)
            .GroupBy(d => new { d.Subject!.Id, d.Subject.Name })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Name,
                DocumentCount = g.Count()
            })
            .OrderByDescending(s => s.DocumentCount)
            .Take(6)
            .ToListAsync();

        return Ok(subjects);
    }

    /// <summary>
    ///     Determines the file type based on file extension
    /// </summary>
    private string GetFileType(string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            ".pdf" => "pdf",
            ".doc" or ".docx" => "document",
            ".ppt" or ".pptx" => "presentation",
            ".xls" or ".xlsx" => "spreadsheet",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "image",
            _ => "other"
        };
    }
}