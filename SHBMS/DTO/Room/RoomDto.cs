namespace SHBMS.DTO.Room;

public class RoomDto
{
    public string RoomId { get; set; } = "";
    public string RoomNumber { get; set; } = "";
    public string RoomTypeId { get; set; } = "";
    public string RoomTypeName { get; set; } = "";
    public decimal BasePrice { get; set; }

    // Operational status is stored separately from occupancy.
    public RoomStatusEnum Status { get; set; }
    public string? RoomImage { get; set; }

    // Calculated from CHECKED_IN bookings rather than an editable status value.
    public bool IsOccupied { get; set; }
    public string CurrentGuestName { get; set; } = "";

    // An empty ACTIVE room displays Available; future reservations still affect booking searches.
    public string DisplayStatus => IsOccupied ? "Occupied" : Status == RoomStatusEnum.Active ? "Available" : Status.ToString();
}
