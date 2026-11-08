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

    // Display value for the end of arrival day; check-in rules compare dates, not hours.
    public DateTime? CheckInDeadline { get; set; }
    public string Status { get; set; } = "";

    // Count Pending and InProgress orders for the checkout hint, without adding charges.
    public int OpenServiceRequestCount { get; set; }
    public string? CancellationReason { get; set; }

    // A confirmed booking becomes overdue only after its arrival date has passed.
    public bool IsCheckInOverdue => Status == "CONFIRMED"
        && CheckInDate < DateOnly.FromDateTime(DateTime.Today);

    // A confirmed booking can check in at any time on its arrival date.
    public bool CanCheckIn => Status == "CONFIRMED"
        && CheckInDate == DateOnly.FromDateTime(DateTime.Today)
        && CheckOutDate > DateOnly.FromDateTime(DateTime.Today);
    public int NumberOfNights => CheckOutDate.DayNumber - CheckInDate.DayNumber;
    public int TotalRooms => Rooms.Sum(r => r.Quantity);
    public decimal RoomSubtotal => Rooms.Sum(r => r.LineTotal);

    // Only completed and previously recorded service charges are included here.
    public decimal ServiceSubtotal => Services.Sum(s => s.TotalAmount);
    public decimal TotalAmount => RoomSubtotal + ServiceSubtotal;
    public decimal PaidAmount => Payments.Sum(p => p.Amount);

    // This can be negative for an overpayment; each screen decides how to display it.
    public decimal RemainingAmount => TotalAmount - PaidAmount;
    public List<BookingRoomLineDto> Rooms { get; set; } = new();
    public List<BookingServiceLineDto> Services { get; set; } = new();

    // All recorded payments, including any room balance collected at check-in.
    public List<BookingPaymentDto> Payments { get; set; } = new();
}

public class BookingRoomLineDto
{
    public string RoomTypeId { get; set; } = "";
    public string TypeName { get; set; } = "";
    public int Quantity { get; set; }

    // Saved nightly price for this booking, not the current room type price.
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
