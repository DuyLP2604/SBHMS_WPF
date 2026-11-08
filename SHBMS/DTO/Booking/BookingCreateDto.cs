namespace SHBMS.DTO.Booking;

public class BookingCreateDto
{
    // Choose an existing customer OR supply a new customer, not both.
    public string? CustomerId { get; set; }
    public NewBookingCustomerDto? NewCustomer { get; set; }
    public DateOnly CheckInDate { get; set; }

    // Departure is excluded when checking room availability; at least one night is required.
    public DateOnly CheckOutDate { get; set; }

    // Requested room types and quantities; physical room numbers are assigned at check-in.
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

    // Optional, but it must be unique when supplied for a new customer.
    public string? IdentityNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? NationalityName { get; set; }
}
