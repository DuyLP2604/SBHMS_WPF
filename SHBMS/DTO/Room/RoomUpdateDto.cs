namespace SBHMS.DTO.Room;

public class RoomUpdateDto
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; }
    public int RoomTypeId { get; set; }
    public RoomStatusEnum Status { get; set; }
}
