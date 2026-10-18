namespace SBHMS.DTO.Guest;

public class CheckInDto
{
    public int? BookingId { get; set; }
    public int CustomerId { get; set; }
    public int RoomId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime ExpectedCheckOutTime { get; set; }
    public int StaffId { get; set; }
}