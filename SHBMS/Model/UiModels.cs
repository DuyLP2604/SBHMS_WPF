namespace SHBMS.Model;

public sealed class UiBooking
{
    public string BookingId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string EmployeeId { get; set; } = "NV0001";
    public string EmployeeName { get; set; } = "Staff preview";
    public DateTime BookingDate { get; set; } = DateTime.Today.AddDays(-3);
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public DateTime CheckInDeadline => CheckInDate.AddHours(14).AddMinutes(15);
    public string Status { get; set; } = "CONFIRMED";
    public string CancellationReason { get; set; } = "";
    public int TotalRooms { get; set; } = 1;
    public int NumberOfNights => (CheckOutDate - CheckInDate).Days;
    public decimal RoomSubtotal => TotalRooms * 700000m * NumberOfNights;
    public decimal ServiceSubtotal { get; set; }
    public decimal TotalEstimatedAmount => RoomSubtotal + ServiceSubtotal;
    public bool IsOverdueCheckIn { get; set; }
}

public sealed class UiBookingDetail
{
    public UiBooking Booking { get; set; } = new();
    public string IdentityNumber { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerAddress { get; set; } = "";
    public string Nationality { get; set; } = "Vietnamese";
    public IReadOnlyList<UiRoomLine> Rooms { get; set; } = Array.Empty<UiRoomLine>();
    public IReadOnlyList<UiServiceLine> Services { get; set; } = Array.Empty<UiServiceLine>();
    public IReadOnlyList<UiPayment> Payments { get; set; } = Array.Empty<UiPayment>();
    public IReadOnlyList<UiInvoice> Invoices { get; set; } = Array.Empty<UiInvoice>();
    public IReadOnlyList<UiAssignment> Assignments { get; set; } = Array.Empty<UiAssignment>();
}

public sealed class UiRoomType
{
    public string RoomTypeId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal BasePrice { get; set; }
    public int Capacity { get; set; }
    public int TotalRooms { get; set; }
    public int BookingReferences { get; set; }
    public int AvailableRooms { get; set; }
    public string DisplayName => $"{Name} · {BasePrice:N0} VND/night · {AvailableRooms} available";
    public bool CanDelete => TotalRooms == 0 && BookingReferences == 0;
}

public sealed class UiRoom
{
    public string RoomId { get; set; } = "";
    public string RoomNumber { get; set; } = "";
    public string RoomTypeName { get; set; } = "";
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string Description { get; set; } = "";
}

public sealed record UiRoomLine(string RoomTypeId, string RoomTypeName, int Capacity,
    int Quantity, decimal UnitPrice, int NumberOfNights)
{
    public decimal LineTotal => Quantity * UnitPrice * NumberOfNights;
}

public sealed record UiCustomer(string CustomerId, string FullName, string Phone)
{
    public string DisplayName => $"{CustomerId} · {FullName} · {Phone}";
}

public sealed record UiServiceLine(string ServiceName, int Quantity, decimal UnitPrice, decimal LineTotal);
public sealed record UiPayment(string PaymentId, DateTime PaymentTime, decimal Amount, string Method, string EmployeeName, string Note);
public sealed record UiInvoice(string InvoiceId, DateTime InvoiceDate, decimal TotalAmount, string EmployeeName);
public sealed record UiAssignment(string RoomNumber, string RoomTypeName, string EmployeeName, DateTime AssignedAt);
