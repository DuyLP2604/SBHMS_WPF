namespace SHBMS.Model;

public partial class Room
{
    public string RoomId { get; set; } = null!;

    public string RoomNumber { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string RoomTypeId { get; set; } = null!;

    public virtual ICollection<RoomAssignment> RoomAssignments { get; set; } = new List<RoomAssignment>();

    public virtual RoomType RoomType { get; set; } = null!;
}
