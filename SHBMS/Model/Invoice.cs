namespace SHBMS.Model;

public partial class Invoice
{
    public string InvoiceId { get; set; } = null!;

    public string BookingId { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string IssuedByEmployee { get; set; } = null!;

    public virtual Booking Booking { get; set; } = null!;

    public virtual Employee IssuedByEmployeeNavigation { get; set; } = null!;
}
