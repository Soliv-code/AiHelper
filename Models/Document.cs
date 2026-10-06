using AiHelper.Models;
using System.ComponentModel.DataAnnotations.Schema;
public partial class Document
{
    public Guid Id { get; set; }
    public Guid KbId { get; set; }
    public string FileName { get; set; } = null!;
    public string FileContent { get; set; } = null!;
    public DateTime? UploadedAt { get; set; }

    // === НОВЫЕ ПОЛЯ С ЯВНЫМ МАППИНГОМ НА SNAKE_CASE ===
    [Column("content_hash")]
    public string? ContentHash { get; set; }

    [Column("source_path")]
    public string? SourcePath { get; set; }

    [Column("file_size")]
    public long? FileSize { get; set; }

    // === НАВИГАЦИОННЫЕ СВОЙСТВА ===
    public virtual KnowledgeBase Kb { get; set; } = null!;
    public virtual ICollection<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();
}