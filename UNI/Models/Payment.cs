using System;
using System.Collections.Generic;

namespace UNI.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? UserId { get; set; }

    public int? CourseId { get; set; }

    public decimal PaymentAmount { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? PaymentDate { get; set; }

    public virtual Course? Course { get; set; }

    public virtual User? User { get; set; }
}
