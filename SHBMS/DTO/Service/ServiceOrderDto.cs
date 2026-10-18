namespace SBHMS.DTO;

public class ServiceOrderDto
{
    public int OrderId { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}