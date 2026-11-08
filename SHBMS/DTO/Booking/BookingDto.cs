namespace SHBMS.DTO.Booking;
public class BookingDto
{
    public string BookingId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public string CreatedByEmployeeName { get; set; } = "";
    public DateTime BookingDate { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public DateTime? CheckInDeadline { get; set; }
    public string Status { get; set; } = "";
    public string? CancellationReason { get; set; }
    public int NumberOfNights => CheckOutDate.DayNumber - CheckInDate.DayNumber;
    public int TotalRooms => Rooms.Sum(r => r.Quantity);
    public decimal RoomSubtotal => Rooms.Sum(r => r.LineTotal);
    public decimal ServiceSubtotal => Services.Sum(s => s.TotalAmount);
    public decimal TotalAmount => RoomSubtotal + ServiceSubtotal;
    public decimal PaidAmount => Payments.Sum(p => p.Amount);
    public decimal RemainingAmount => TotalAmount - PaidAmount;
    public List<BookingRoomLineDto> Rooms { get; set; } = new();
    public List<BookingServiceLineDto> Services { get; set; } = new();
    public List<BookingPaymentDto> Payments { get; set; } = new();
}
public class BookingRoomLineDto
{
    public string RoomTypeId { get; set; } = "";
    public string TypeName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int NumberOfNights { get; set; }
    public decimal LineTotal => Quantity * UnitPrice * NumberOfNights;
    public string AssignedRooms { get; set; } = "";
}
public class BookingServiceLineDto
{
    public string ServiceName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount => Quantity * UnitPrice;
}
public class BookingPaymentDto
{
    public DateTime PaymentTime { get; set; }
    public string MethodName { get; set; } = "";
    public decimal Amount { get; set; }
}
