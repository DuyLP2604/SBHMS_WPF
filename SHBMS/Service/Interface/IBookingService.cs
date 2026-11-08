using SHBMS.DTO.Booking;
namespace SHBMS.Service.Interface;
public interface IBookingService
{
    Task<List<BookingDto>> GetAllBookingsAsync();
    Task<BookingDto> GetBookingByIdAsync(string bookingId);
    Task<List<BookingCustomerDto>> SearchCustomersAsync(string phone);
    Task<List<AvailableRoomTypeDto>> GetAvailableRoomTypesAsync(DateOnly arrival, DateOnly departure);
    Task<string> CreateBookingAsync(BookingCreateDto dto);
}
