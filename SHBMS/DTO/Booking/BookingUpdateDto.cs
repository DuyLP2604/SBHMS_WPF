namespace SBHMS.DTO.Booking;

public class BookingUpdateDto
{
    public int BookingId { get; set; }
    public int RoomTypeId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public string Note { get; set; }
}