using System;
using System.Collections.Generic;

namespace SHBMS.Model;

public partial class Complaint
{
    public int ComplaintId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public string? CustomerId { get; set; }

    public string? ReplyMessage { get; set; }

    public string? EmployeeId { get; set; }

    public string? ResolvedByEmployee { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Employee? ResolvedByEmployeeNavigation { get; set; }
}
