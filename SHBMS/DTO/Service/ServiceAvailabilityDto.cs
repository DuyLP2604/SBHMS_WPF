
namespace SHBMS.DTO.Service;

public class ServiceAvailabilityDto
{
    public string ServiceId { get; set; } = "";

    public string ServiceName { get; set; } = "";

    public decimal UnitPrice { get; set; }

    public bool IsAvailable { get; set; }

    public int? StockQuantity { get; set; }

    public string AvailabilityStatus =>
        IsAvailable ? "Available" : "Unavailable";

    public string StockDisplay =>
        StockQuantity?.ToString() ?? "Not tracked";
}
