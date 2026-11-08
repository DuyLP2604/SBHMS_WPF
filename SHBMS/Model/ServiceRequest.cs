
using System;

namespace SHBMS.Model;

public class ServiceRequest
{
    public int RequestId { get; set; }

    public string BookingId { get; set; } = "";

    public string ServiceId { get; set; } = "";

    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public DateTime? BilledAt { get; set; }

    public string RequestDescription { get; set; } = "";

    public string RequestStatus { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string? HandledByEmployeeId { get; set; }
}
