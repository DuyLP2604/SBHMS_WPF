namespace SHBMS.DTO.Invoice;

public class PaymentCreateDto
{
    public string InvoiceId { get; set; } = "";
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = ""; // "Cash", "CreditCard", "Banking"
    public string ProcessedByStaffId { get; set; } = "";
}
