using SHBMS.DTO.Booking;

namespace SHBMS.Service.Interface;

public interface IBookingService
{
    /// <summary>
    /// Read booking list and detail data for the available screens.
    /// </summary>
    Task<List<BookingDto>> GetAllBookingsAsync();

    /// <summary>
    /// Read one booking by its generated booking ID.
    /// </summary>
    Task<BookingDto> GetBookingByIdAsync(string bookingId);

    /// <summary>
    /// Find customers whose phone number contains the search text.
    /// </summary>
    Task<List<BookingCustomerDto>> SearchCustomersAsync(string phone);

    /// <summary>
    /// Quote room type availability and prices for the requested dates.
    /// </summary>
    Task<List<AvailableRoomTypeDto>> GetAvailableRoomTypesAsync(DateOnly arrival, DateOnly departure);

    /// <summary>
    /// Save a validated booking and return its new booking ID.
    /// </summary>
    Task<string> CreateBookingAsync(BookingCreateDto dto);
}
