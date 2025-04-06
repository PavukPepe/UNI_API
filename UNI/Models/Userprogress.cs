using System;
using System.Collections.Generic;

namespace UNI.Models;

public partial class Userprogress
{
    public int ProgressId { get; set; }

    public int? UserId { get; set; }

    public int? StepId { get; set; }

    public bool? IsCompleted { get; set; }

    public DateTime? CompletionDate { get; set; }

    public virtual Step? Step { get; set; }

    public virtual User? User { get; set; }
}
