
using System;

namespace SHBMS.DTO.Service;

public class GuestServiceRequest
{
    public int Id { get; set; }

    public string BookingId { get; set; } = "";

    public string ServiceId { get; set; } = "";

    public string CustomerId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public bool IsLegacy { get; set; }

    public string ServiceName { get; set; } = "";

    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount => Quantity * UnitPrice;
    public DateTime? BilledAt { get; set; }

    public string Details { get; set; } = "";

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string HandledBy { get; set; } = "";
}
