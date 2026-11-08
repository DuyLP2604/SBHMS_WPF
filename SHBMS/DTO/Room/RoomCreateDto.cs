namespace SHBMS.DTO.Room;
public class RoomCreateDto
{
    public string RoomNumber { get; set; } = "";
    public string RoomTypeId { get; set; } = "";
    public RoomStatusEnum Status { get; set; } = RoomStatusEnum.Active;
    public string? RoomImage { get; set; }
}
