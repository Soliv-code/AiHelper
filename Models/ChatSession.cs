namespace AiHelper.Models;

public partial class ChatSession
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string ModelName { get; set; } = null!;
    public string? Title { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    public virtual User? User { get; set; }
    public virtual ICollection<KnowledgeBase> Kbs { get; set; } = new List<KnowledgeBase>();
}
