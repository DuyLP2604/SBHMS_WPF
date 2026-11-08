namespace SHBMS.DTO.Room;

public class RoomTypeCreateDto
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    // Price per room per night. Existing bookings keep their saved unit price.
    public decimal BasePrice { get; set; }

    // Guest capacity cannot be reduced while this type has unfinished bookings.
    public int Capacity { get; set; }
    public string? RoomTypeImage { get; set; }
}
