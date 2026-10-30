namespace SHBMS.Model;

public partial class Payment
{
    public string PaymentId { get; set; } = null!;

    public string BookingId { get; set; } = null!;

    public decimal Amount { get; set; }

    public string MethodId { get; set; } = null!;

    public DateTime PaymentTime { get; set; }

    public string CollectedByEmployee { get; set; } = null!;

    public string? Note { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Employee CollectedByEmployeeNavigation { get; set; } = null!;

    public virtual Paymentmethod Method { get; set; } = null!;
}
