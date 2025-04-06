using System;
using System.Collections.Generic;

namespace UNI.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int? CourseId { get; set; }

    public int? UserId { get; set; }

    public int? UserRating { get; set; }

    public string? ReviewText { get; set; }

    public DateTime? SubmissionDate { get; set; }

    public virtual Course? Course { get; set; }

    public virtual User? User { get; set; }
}
