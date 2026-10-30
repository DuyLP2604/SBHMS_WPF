namespace SHBMS.Model;

public partial class RoomAssignment
{
    public int AssignmentId { get; set; }

    public int BookingDetailId { get; set; }

    public string RoomId { get; set; } = null!;

    public string AssignedByEmployee { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public virtual Employee AssignedByEmployeeNavigation { get; set; } = null!;

    public virtual BookingDetail BookingDetail { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;
}
