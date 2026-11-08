namespace SHBMS.DTO.Guest;

// A booking seen as a guest stay: who, when, which rooms, what it costs so far. Read-only.
public class GuestStayDto
{
    public string BookingId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public DateTime CheckInDeadline { get; set; }
    // Raw BOOKING.BookingStatus: PENDING, CONFIRMED, CHECKED_IN, CHECKED_OUT, CANCELLED.
    public string Status { get; set; } = "";
    // Front-desk label: Upcoming, Arriving today, Overdue check-in, In house, Due out today, ...
    public string Phase { get; set; } = "";
    public decimal PaidAmount { get; set; }
    public List<StayRoomLineDto> Rooms { get; set; } = new();
    public List<StayServiceLineDto> Services { get; set; } = new();

    public int Nights => CheckOutDate.DayNumber - CheckInDate.DayNumber;
    public string RoomNumbers => string.Join(", ", Rooms.SelectMany(r => r.AssignedRooms));
    public decimal RoomCharge => Rooms.Sum(r => r.Quantity * r.UnitPrice) * Nights;
    public decimal ServiceCharge => Services.Sum(s => s.Quantity * s.UnitPrice);
    public decimal TotalCharge => RoomCharge + ServiceCharge;
    public decimal Balance => TotalCharge - PaidAmount;
}

public class StayRoomLineDto
{
    public string RoomTypeName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public List<string> AssignedRooms { get; set; } = new();
    public string Text => $"{RoomTypeName} x {Quantity} @ {UnitPrice:N0} / night"
        + (AssignedRooms.Count > 0 ? $"  [{string.Join(", ", AssignedRooms)}]" : "  [no room assigned yet]");
}

public class StayServiceLineDto
{
    public string ServiceName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Text => $"{ServiceName} x {Quantity} @ {UnitPrice:N0}";
}
