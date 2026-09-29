using System;
using System.Collections.Generic;

namespace AiHelper.Models;

public partial class ChatMessage
{
    public Guid Id { get; set; }

    public Guid ChatSessionId { get; set; }

    public string Role { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ChatSession ChatSession { get; set; } = null!;
}
