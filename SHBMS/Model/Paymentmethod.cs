namespace SHBMS.Model;

public partial class Paymentmethod
{
    public string MethodId { get; set; } = null!;

    public string MethodName { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
