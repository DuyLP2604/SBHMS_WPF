namespace SHBMS.DTO.Booking;

public class BookingUpdateDto
{
    public string BookingId { get; set; } = "";
    public string RoomTypeId { get; set; } = "";
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public string Note { get; set; } = "";
}
