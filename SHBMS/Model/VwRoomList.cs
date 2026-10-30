namespace SHBMS.Model;

public partial class VwRoomList
{
    public string RoomId { get; set; } = null!;

    public string RoomNumber { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string RoomTypeId { get; set; } = null!;

    public string TypeName { get; set; } = null!;

    public int Capacity { get; set; }

    public decimal Price { get; set; }

    public string? Description { get; set; }
}
