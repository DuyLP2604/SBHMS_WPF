namespace SBHMS.DTO.Invoice;

public class PaymentCreateDto
{
    public int InvoiceId { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } // "Cash", "CreditCard", "Banking"
    public int ProcessedByStaffId { get; set; }
}
