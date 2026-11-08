namespace SHBMS.DTO.Guest;

public class CheckInDto
{
    public string? BookingId { get; set; }
    public string CustomerId { get; set; } = "";
    public string RoomId { get; set; } = "";
    public DateTime CheckInTime { get; set; }
    public DateTime ExpectedCheckOutTime { get; set; }
    public string StaffId { get; set; } = "";
}
