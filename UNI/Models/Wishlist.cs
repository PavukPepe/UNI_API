using System;
using System.Collections.Generic;

namespace UNI.Models;

public partial class Wishlist
{
    public int WishlistId { get; set; }

    public int? UserId { get; set; }

    public int? CourseId { get; set; }

    public DateTime? AddedDate { get; set; }

    public virtual Course? Course { get; set; }

    public virtual User? User { get; set; }
}
