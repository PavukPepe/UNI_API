using System;
using System.Collections.Generic;

namespace UNI.Models;

public partial class Topic
{
    public int TopicId { get; set; }

    public int? BlockId { get; set; }

    public string TopicTitle { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual Block? Block { get; set; }

    public virtual ICollection<Step> Steps { get; set; } = new List<Step>();
}
