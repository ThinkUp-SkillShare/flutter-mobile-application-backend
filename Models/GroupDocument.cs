using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models;

[Table("group_documents")]
public class GroupDocument
{
    [Key] 
    [Column("id")] 
    public int Id { get; set; }

    [Required] 
    [Column("group_id")] 
    public int GroupId { get; set; }

    [Required] 
    [Column("user_id")] 
    public int UserId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")] 
    public string? Description { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    [Column("file_size")] 
    public long? FileSize { get; set; }

    [MaxLength(50)] 
    [Column("file_type")] 
    public string? FileType { get; set; }

    [Column("subject_id")] 
    public int? SubjectId { get; set; }

    [Column("upload_date")] 
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    [Column("download_count")] 
    public int DownloadCount { get; set; } = 0;

    [Column("favorite_count")] 
    public int? FavoriteCount { get; set; } = 0;

    [ForeignKey("GroupId")] 
    public virtual StudyGroup? Group { get; set; }

    [ForeignKey("UserId")] 
    public virtual User? User { get; set; }

    [ForeignKey("SubjectId")] 
    public virtual Subject? Subject { get; set; }
}