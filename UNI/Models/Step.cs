using System;
using System.Collections.Generic;

namespace UNI.Models;

public partial class Step
{
    public int StepId { get; set; }

    public int? TopicId { get; set; }

    public string StepTitle { get; set; } = null!;

    public string? ContentType { get; set; }

    public string? StepContent { get; set; }

    public int DisplayOrder { get; set; }

    public virtual Topic? Topic { get; set; }

    public virtual ICollection<Userprogress> Userprogresses { get; set; } = new List<Userprogress>();
}
