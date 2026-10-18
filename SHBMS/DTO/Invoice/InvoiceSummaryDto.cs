namespace SBHMS.DTO.Invoice;

public class InvoiceSummaryDto
{
    public int StayId { get; set; }
    public decimal TotalRoomCharge { get; set; }
    public decimal TotalServiceCharge { get; set; }
    public decimal GrandTotal { get; set; }
}
