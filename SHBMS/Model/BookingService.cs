namespace SHBMS.Model;

public partial class BookingService
{
    public string BookingId { get; set; } = null!;

    public string ServiceId { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public string OrderedByEmployee { get; set; } = null!;

    public virtual Booking Booking { get; set; } = null!;

    public virtual Employee OrderedByEmployeeNavigation { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
