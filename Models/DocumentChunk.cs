using System;
using System.Collections.Generic;
using Pgvector;

namespace AiHelper.Models;

public partial class DocumentChunk
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string? BreadcrumbPath { get; set; }

    public string ChunkText { get; set; } = null!;

    public Vector? Embedding { get; set; }

    public virtual Document Document { get; set; } = null!;
}
