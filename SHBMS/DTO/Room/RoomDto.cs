namespace SBHMS.DTO.Room;

public class RoomDto
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; }
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; }
    public decimal BasePrice { get; set; }
    public RoomStatusEnum Status { get; set; }
}
