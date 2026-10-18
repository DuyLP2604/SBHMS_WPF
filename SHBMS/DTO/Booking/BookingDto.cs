namespace SBHMS.DTO.Booking;

public class BookingDto
{
    public int BookingId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string CustomerPhone { get; set; }
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public string Status { get; set; } 
    public decimal TotalEstimatedAmount { get; set; }
    public string Note { get; set; }
}
