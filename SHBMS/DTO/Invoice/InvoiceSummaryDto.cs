namespace SHBMS.DTO.Invoice;

public class InvoiceSummaryDto
{
    public string StayId { get; set; } = "";
    public decimal TotalRoomCharge { get; set; }
    public decimal TotalServiceCharge { get; set; }
    public decimal GrandTotal { get; set; }
}
