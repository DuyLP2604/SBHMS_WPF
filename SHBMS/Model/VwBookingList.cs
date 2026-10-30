namespace SHBMS.Model;

public partial class VwBookingList
{
    public string BookingId { get; set; } = null!;

    public string CustomerId { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string EmployeeId { get; set; } = null!;

    public string CreatedByEmployeeName { get; set; } = null!;

    public DateTime BookingDate { get; set; }

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public DateTime? CheckInDeadlineOverride { get; set; }

    public DateTime? CheckInDeadline { get; set; }

    public string BookingStatus { get; set; } = null!;

    public string? CancellationReason { get; set; }

    public int? NumberOfNights { get; set; }

    public int TotalRooms { get; set; }

    public decimal? RoomSubtotal { get; set; }

    public decimal ServiceSubtotal { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public bool? IsOverdueCheckIn { get; set; }
}
