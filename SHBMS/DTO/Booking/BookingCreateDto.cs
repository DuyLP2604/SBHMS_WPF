namespace SHBMS.DTO.Booking;
public class BookingCreateDto
{
    // Choose an existing customer OR supply a new customer, not both.
    public string? CustomerId { get; set; }
    public NewBookingCustomerDto? NewCustomer { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public List<BookingRoomRequestDto> Rooms { get; set; } = new();
}
public class BookingRoomRequestDto
{
    public string RoomTypeId { get; set; } = "";
    public int Quantity { get; set; }
    // Only used to detect a changed quote. The service always saves the database price.
    public decimal QuotedUnitPrice { get; set; }
}
public class NewBookingCustomerDto
{
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? IdentityNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? NationalityName { get; set; }
}
