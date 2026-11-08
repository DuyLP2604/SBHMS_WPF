namespace SHBMS.DTO.Room;

public class RoomTypeDto : RoomTypeUpdateDto
{
    public int RoomCount { get; set; }

    // Used to explain why a type must be kept even when it has no current rooms.
    public bool HasBookingHistory { get; set; }
}
