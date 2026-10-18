using SBHMS.DTO.Booking;
using SBHMS.Services.Interfaces;

namespace SBHMS.Services.Implementations
{

    public class BookingService : IBookingService
    {
        private readonly PRN212_SBHMSContext _context;
        public BookingService(PRN212_SBHMSContext context) { _context = context; }

        public Task<IEnumerable<BookingDto>> GetAllBookingsAsync() => throw new NotImplementedException();
        public Task<BookingDto> GetBookingByIdAsync(int bookingId) => throw new NotImplementedException();
        public Task<bool> CreateBookingAsync(BookingCreateDto dto) => throw new NotImplementedException();
        public Task<bool> UpdateBookingAsync(BookingUpdateDto dto) => throw new NotImplementedException();
        public Task<bool> CancelBookingAsync(int bookingId, string reason) => throw new NotImplementedException();
        public Task<IEnumerable<BookingDto>> GetPendingReviewBookingsAsync() => throw new NotImplementedException();
        public Task<bool> ApproveBookingAsync(int bookingId, int managerId) => throw new NotImplementedException();
    }
}