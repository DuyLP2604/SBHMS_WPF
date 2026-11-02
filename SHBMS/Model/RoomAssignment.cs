using System;
using System.Collections.Generic;

namespace SHBMS.Model;

public partial class RoomAssignment
{
    public int AssignmentId { get; set; }

    public int BookingDetailId { get; set; }

    public string RoomId { get; set; } = null!;

    public string? EmployeeId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual BookingDetail BookingDetail { get; set; } = null!;

    public virtual Employee? Employee { get; set; }

    public virtual Room Room { get; set; } = null!;
}
