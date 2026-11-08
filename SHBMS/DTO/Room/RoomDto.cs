namespace SHBMS.DTO.Room;
public class RoomDto
{
    public string RoomId { get; set; } = "";
    public string RoomNumber { get; set; } = "";
    public string RoomTypeId { get; set; } = "";
    public string RoomTypeName { get; set; } = "";
    public decimal BasePrice { get; set; }
    public RoomStatusEnum Status { get; set; }
    public string? RoomImage { get; set; }
    public bool IsOccupied { get; set; }
    public string CurrentGuestName { get; set; } = "";
    public string DisplayStatus => IsOccupied ? "Occupied" : Status == RoomStatusEnum.Active ? "Available" : Status.ToString();
}
