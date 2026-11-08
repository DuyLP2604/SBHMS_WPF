namespace SHBMS.DTO.Room;
public class RoomTypeDto : RoomTypeUpdateDto
{
    public int RoomCount { get; set; }
    public bool HasBookingHistory { get; set; }
}
