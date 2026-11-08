namespace SHBMS.DTO.Room;

// Live view of one room: operational status plus who is in it or arriving today.
public class RoomStatusDto
{
    public string RoomId { get; set; } = "";
    public string RoomNumber { get; set; } = "";
    public string RoomTypeName { get; set; } = "";
    public RoomStatusEnum Status { get; set; }
    public string CurrentGuestName { get; set; } = "";

    public string RoomTypeId { get; set; } = "";
    public bool IsOccupied { get; set; }
    public bool IsReserved { get; set; }
    public string ReservedGuestName { get; set; } = "";
    public string BookingId { get; set; } = "";
    public DateOnly? ExpectedCheckOut { get; set; }

    // A guest in the room always wins over the operational flag.
    public string DisplayStatus => IsOccupied ? "Occupied"
        : Status == RoomStatusEnum.Active ? (IsReserved ? "Reserved" : "Available")
        : Status.ToString();
    public string GuestText => IsOccupied ? CurrentGuestName
        : IsReserved ? ReservedGuestName + " (arriving)" : "";
    public string DetailText => IsOccupied && ExpectedCheckOut != null
        ? $"Out {ExpectedCheckOut:dd/MM/yyyy}" : "";
}
