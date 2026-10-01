using System;
using System.Collections.Generic;

namespace AiHelper.Models;

public partial class UserPreference
{
    public Guid UserId { get; set; }

    public string? LastModelName { get; set; }

    public virtual User User { get; set; } = null!;
}
