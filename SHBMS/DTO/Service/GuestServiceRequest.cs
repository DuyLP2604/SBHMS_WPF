
using System;

namespace SHBMS.DTO.Service;

public class GuestServiceRequest
{
    public int Id { get; set; }

    public string BookingId { get; set; } = "";

    public string ServiceId { get; set; } = "";

    public string ServiceName { get; set; } = "";

    public string Details { get; set; } = "";

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string HandledBy { get; set; } = "";
}
