using System;
using System.Collections.Generic;

namespace AiHelper.Models;

public partial class Document
{
    public Guid Id { get; set; }

    public Guid KbId { get; set; }

    public string FileName { get; set; } = null!;

    public string FileContent { get; set; } = null!;

    public DateTime? UploadedAt { get; set; }

    public virtual ICollection<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();

    public virtual KnowledgeBasis Kb { get; set; } = null!;
}
