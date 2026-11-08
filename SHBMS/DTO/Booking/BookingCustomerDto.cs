namespace SHBMS.DTO.Booking;
public class BookingCustomerDto
{
    public string CustomerId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? IdentityNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? NationalityName { get; set; }
}
