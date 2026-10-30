namespace SHBMS.Model;

public partial class Booking
{
    public string BookingId { get; set; } = null!;

    public string CustomerId { get; set; } = null!;

    public string EmployeeId { get; set; } = null!;

    public DateTime BookingDate { get; set; }

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public string BookingStatus { get; set; } = null!;

    public string? CancellationReason { get; set; }

    public DateTime? CheckInDeadlineOverride { get; set; }

    public DateTime? CheckInDeadline { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByEmployee { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();

    public virtual Customer Customer { get; set; } = null!;

    public virtual Employee? DeletedByEmployeeNavigation { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
