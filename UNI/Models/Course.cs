using System;
using System.Collections.Generic;

namespace UNI.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public string CourseTitle { get; set; } = null!;

    public string? CourseDescription { get; set; }

    public int? AuthorId { get; set; }

    public int? CategoryId { get; set; }

    public decimal? CoursePrice { get; set; }

    public decimal? AverageRating { get; set; }

    public string? CourseLogo { get; set; }

    public string? DifficultyLevel { get; set; }

    public int? DurationHours { get; set; }

    public string? CourseLanguage { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? CreationDate { get; set; }

    public virtual User? Author { get; set; }

    public virtual ICollection<Block> Blocks { get; set; } = new List<Block>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
