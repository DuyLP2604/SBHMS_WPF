namespace SHBMS.DTO.Booking;

public class AvailableRoomTypeDto
{
    public string RoomTypeId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Capacity { get; set; }

    // Current nightly price quoted by the room search.
    public decimal UnitPrice { get; set; }

    // Quantity available for the full requested stay, including unassigned reservations.
    public int AvailableRooms { get; set; }
}
