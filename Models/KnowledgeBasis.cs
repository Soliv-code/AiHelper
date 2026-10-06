using System;
using System.Collections.Generic;

namespace AiHelper.Models;

public partial class KnowledgeBasis
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsPublic { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
}
