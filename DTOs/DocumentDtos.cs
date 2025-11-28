using System.ComponentModel.DataAnnotations;

namespace SkillShareBackend.DTOs;

public class GroupDocumentDto
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;

    [Required] public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required] public string FileName { get; set; } = string.Empty;

    [Required] public string FileUrl { get; set; } = string.Empty;

    public long? FileSize { get; set; }
    public string? FileType { get; set; }
    public int? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public DateTime UploadDate { get; set; }
    public int DownloadCount { get; set; }
}

public class UploadDocumentDto
{
    [Required] public int GroupId { get; set; }

    [Required] public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? SubjectId { get; set; }

    [Required] public IFormFile File { get; set; } = null!;
}

public class UpdateDocumentDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? SubjectId { get; set; }
}

public class DocumentStatisticsDto
{
    public int TotalDocuments { get; set; }
    public long TotalSize { get; set; }
    public int PdfCount { get; set; }
    public int DocumentCount { get; set; }
    public int PresentationCount { get; set; }
    public int SpreadsheetCount { get; set; }
    public int ImageCount { get; set; }
    public int OtherCount { get; set; }
}