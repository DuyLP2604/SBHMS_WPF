namespace SHBMS.DTO.Room;

public class RoomStatusDto
{
    public string RoomId { get; set; } = "";
    public string RoomNumber { get; set; } = "";
    public string RoomTypeName { get; set; } = "";
    public RoomStatusEnum Status { get; set; }
    public string CurrentGuestName { get; set; } = "";
}
