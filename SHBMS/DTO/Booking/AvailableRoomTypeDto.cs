namespace SHBMS.DTO.Booking;
public class AvailableRoomTypeDto
{
    public string RoomTypeId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Capacity { get; set; }
    public decimal UnitPrice { get; set; }
    public int AvailableRooms { get; set; }
}
