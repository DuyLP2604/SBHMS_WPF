namespace SHBMS.Model;

public partial class Complaint
{
    public int ComplaintId { get; set; }

    public string CustomerId { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string RecordedByEmployee { get; set; } = null!;

    public string? ResolvedByEmployee { get; set; }

    public string? ReplyMessage { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Employee RecordedByEmployeeNavigation { get; set; } = null!;
}
