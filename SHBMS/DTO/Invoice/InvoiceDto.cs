namespace SBHMS.DTO.Invoice;

public class InvoiceDto
{
    public int InvoiceId { get; set; }
    public int StayId { get; set; }
    public string CustomerName { get; set; }
    public string RoomNumber { get; set; }
    public decimal RoomCharge { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsPaid { get; set; }
    public List<ServiceOrderDto> ServiceOrders { get; set; } = new List<ServiceOrderDto>();
}
