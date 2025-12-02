using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;

namespace SkillShareBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(AppDbContext context, ILogger<DocumentController> logger, IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    private int GetUserId()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("uid")?.Value
                              ?? User.FindFirst("userId")?.Value
                              ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("User ID not found in token");

            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user ID from token");
            throw new UnauthorizedAccessException("Failed to extract user ID from token");
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

            // Verificar que el usuario es miembro del grupo
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
                    DownloadCount = d.DownloadCount
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
            var userId = GetUserId();

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
                    DownloadCount = d.DownloadCount
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
            _logger.LogError(ex, "Error getting user documents");
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

            // Verificar que el usuario es miembro del grupo
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == document.GroupId && gm.UserId == userId);

            if (!isMember) return Forbid();

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
                DownloadCount = document.DownloadCount
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

            if (dto.File == null || dto.File.Length == 0) return BadRequest(new { message = "No file uploaded" });

            // Validar tamaño del archivo (max 50MB)
            if (dto.File.Length > 50 * 1024 * 1024) return BadRequest(new { message = "File size exceeds 50MB limit" });

            // Crear directorio de uploads si no existe
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
            if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

            // Generar nombre único para el archivo
            var fileExtension = Path.GetExtension(dto.File.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Guardar archivo
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            // Determinar tipo de archivo
            var fileType = GetFileType(fileExtension);

            // Verificar que el subject existe si se proporcionó
            if (dto.SubjectId.HasValue)
            {
                var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == dto.SubjectId.Value);
                if (!subjectExists)
                {
                    // Eliminar archivo subido si el subject no existe
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                    return BadRequest(new { message = "Invalid subject ID" });
                }
            }

            // Crear documento en base de datos
            var document = new GroupDocument
            {
                GroupId = dto.GroupId,
                UserId = userId,
                Title = dto.Title,
                Description = dto.Description,
                FileName = dto.File.FileName,
                FileUrl = $"/uploads/documents/{fileName}",
                FileSize = dto.File.Length,
                FileType = fileType,
                SubjectId = dto.SubjectId,
                UploadDate = DateTime.UtcNow,
                DownloadCount = 0
            };

            _context.GroupDocuments.Add(document);
            await _context.SaveChangesAsync();

            // Cargar relaciones para el response
            await _context.Entry(document)
                .Reference(d => d.Subject)
                .LoadAsync();

            var resultDto = new GroupDocumentDto
            {
                Id = document.Id,
                GroupId = document.GroupId,
                UserId = document.UserId,
                Title = document.Title,
                Description = document.Description,
                FileName = document.FileName,
                FileUrl = document.FileUrl,
                FileSize = document.FileSize,
                FileType = document.FileType,
                SubjectId = document.SubjectId,
                SubjectName = document.Subject?.Name,
                UploadDate = document.UploadDate,
                DownloadCount = document.DownloadCount
            };

            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, resultDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
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

            if (document == null) return NotFound(new { message = "Document not found" });

            // Solo el propietario puede editar
            if (document.UserId != userId) return Forbid();

            if (!string.IsNullOrEmpty(dto.Title))
                document.Title = dto.Title;

            if (dto.Description != null)
                document.Description = dto.Description;

            if (dto.SubjectId.HasValue)
            {
                // Verificar que el subject existe
                var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == dto.SubjectId.Value);
                if (!subjectExists) return BadRequest(new { message = "Invalid subject ID" });
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

    // DELETE: api/document/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        try
        {
            var userId = GetUserId();

            var document = await _context.GroupDocuments.FindAsync(id);
            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            // Solo el propietario puede eliminar
            if (document.UserId != userId)
            {
                return Forbid();
            }

            // Eliminar archivo físico si existe
            if (!string.IsNullOrEmpty(document.FileUrl) && document.FileUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", document.FileUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.GroupDocuments.Remove(document);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", id);
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
            {
                return NotFound(new { message = "Document not found" });
            }

            // Verificar que el usuario es miembro del grupo
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == document.GroupId && gm.UserId == userId);

            if (!isMember)
            {
                return Forbid();
            }

            // Incrementar contador de descargas
            document.DownloadCount++;
            await _context.SaveChangesAsync();

            // Si es un archivo local, servir el archivo
            if (document.FileUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", document.FileUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                    return File(fileBytes, "application/octet-stream", document.FileName);
                }
                else
                {
                    return NotFound(new { message = "File not found on server" });
                }
            }
            else
            {
                // Para URLs externas, redirigir
                return Ok(new { downloadUrl = document.FileUrl });
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", id);
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

            // Verificar que el usuario es miembro del grupo
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (!isMember) return Forbid();

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