namespace SHBMS.DTO.Invoice;

public class PaymentDto
{
    public string PaymentId { get; set; } = "";
    public string InvoiceId { get; set; } = "";
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = "";
    public DateTime PaymentDate { get; set; }
    public string StaffName { get; set; } = "";
}
