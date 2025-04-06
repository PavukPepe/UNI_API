using System;
using System.Collections.Generic;

namespace UNI.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int? CreatedByUser { get; set; }

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual User? CreatedByUserNavigation { get; set; }
}
