using System;
using System.Collections.Generic;

namespace UNI.Models;

public partial class Block
{
    public int BlockId { get; set; }

    public int? CourseId { get; set; }

    public string BlockTitle { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual Course? Course { get; set; }

    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
