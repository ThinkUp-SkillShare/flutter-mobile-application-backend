using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models;

[Table("document_favorites")]
public class DocumentFavorite
{
    [Key] 
    [Column("id")] 
    public int Id { get; set; }

    [Required] 
    [Column("document_id")] 
    public int DocumentId { get; set; }

    [Required] 
    [Column("user_id")] 
    public int UserId { get; set; }

    [Column("created_at")] 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("DocumentId")] 
    public virtual GroupDocument Document { get; set; } = null!;

    [ForeignKey("UserId")] 
    public virtual User User { get; set; } = null!;
}