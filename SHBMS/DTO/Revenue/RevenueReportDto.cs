namespace SBHMS.DTO.Revenue;

public class RevenueReportDto
{
    public DateTime ReportDate { get; set; }
    public decimal RoomRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal TotalRevenue => RoomRevenue + ServiceRevenue;
    public int TotalCheckIns { get; set; }
    public int TotalCheckOuts { get; set; }
}
