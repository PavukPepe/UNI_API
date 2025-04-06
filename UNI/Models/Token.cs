using System;
using System.Collections.Generic;

namespace UNI.Models;

public partial class Token
{
    public int TokenId { get; set; }

    public int? UserId { get; set; }

    public string AccessToken { get; set; } = null!;

    public DateTime ExpirationTime { get; set; }

    public virtual User? User { get; set; }
}
