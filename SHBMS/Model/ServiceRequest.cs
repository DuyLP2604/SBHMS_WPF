namespace SHBMS.Model;

public partial class ServiceRequest
{
    public int RequestId { get; set; }

    public string BookingId { get; set; } = null!;

    public string ServiceId { get; set; } = null!;

    public string RequestDescription { get; set; } = null!;

    public string RequestStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string? HandledByEmployeeId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime? BilledAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Employee? HandledByEmployee { get; set; }

    public virtual Service Service { get; set; } = null!;
}
