namespace SHBMS.Service.Interface
{
    public interface IBookingService
    {
        // [UC-05] Manage Bookings (Staff)
        Task<IEnumerable<BookingDto>> GetAllBookingsAsync();
        Task<BookingDto> GetBookingByIdAsync(int bookingId);
        Task<bool> CreateBookingAsync(BookingCreateDto dto);
        Task<bool> UpdateBookingAsync(BookingUpdateDto dto);
        Task<bool> CancelBookingAsync(int bookingId, string reason);

        // [UC-06] Review Bookings (Manager)
        Task<IEnumerable<BookingDto>> GetPendingReviewBookingsAsync();
        Task<bool> ApproveBookingAsync(int bookingId, int managerId);
    }
}