namespace SHBMS.DTO;

public class ServiceOrderDto
{
    public int OrderId { get; set; }
    public string ServiceId { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}
