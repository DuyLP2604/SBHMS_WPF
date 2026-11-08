using Microsoft.EntityFrameworkCore;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.DTO.Guest;
using SHBMS.DTO.Invoice;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation
{
    public class GuestStayService : IGuestStayService
    {
        private readonly Prn212SbhmsContext _context;
        public GuestStayService(Prn212SbhmsContext? context = null) { _context = context ?? new Prn212SbhmsContext(); }

        // [FE-02] Read-only. Customer name and phone are projected instead of loading the whole
        // Customer entity, so the screen keeps working if the Customer table gains columns.
        public async Task<List<GuestStayDto>> GetStaysAsync()
        {
            if (!Session.CanViewBookings)
                throw new UnauthorizedAccessException("Only Staff and Manager can view guest stays.");
            var account = Session.CurrentAccount!;
            if (!await _context.Users.AnyAsync(u => u.UserId == account.UserId
                && (u.Role == "Staff" || u.Role == "Manager")))
                throw new UnauthorizedAccessException("Your database account cannot view guest stays.");

            DateTime now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var since = today.AddDays(-30);
            // Open and in-house stays are always shown; finished ones only for the last 30 days.
            var bookings = await _context.Bookings.AsNoTracking()
                .Where(b => !b.IsDeleted && (b.BookingStatus == "PENDING" || b.BookingStatus == "CONFIRMED"
                    || b.BookingStatus == "CHECKED_IN" || b.CheckOutDate >= since))
                .OrderBy(b => b.CheckInDate).ThenBy(b => b.BookingId)
                .Select(b => new
                {
                    b.BookingId, b.CustomerId, CustomerName = b.Customer.FullName, CustomerPhone = b.Customer.Phone,
                    b.CheckInDate, b.CheckOutDate, b.BookingStatus
                }).ToListAsync();
            var ids = bookings.Select(b => b.BookingId).ToList();
            var details = await _context.BookingDetails.AsNoTracking().Where(d => ids.Contains(d.BookingId))
                .Select(d => new { d.BookingId, d.BookingDetailId, TypeName = d.RoomType.TypeName, d.Quantity, d.UnitPrice })
                .ToListAsync();
            var assigned = await _context.RoomAssignments.AsNoTracking().Where(a => ids.Contains(a.BookingDetail.BookingId))
                .Select(a => new { a.BookingDetailId, a.Room.RoomNumber }).ToListAsync();
            var services = await _context.BookingServices.AsNoTracking().Where(s => ids.Contains(s.BookingId))
                .Select(s => new { s.BookingId, s.Service.ServiceName, s.Quantity, s.UnitPrice }).ToListAsync();
            var payments = await _context.Payments.AsNoTracking().Where(p => ids.Contains(p.BookingId))
                .Select(p => new { p.BookingId, p.Amount }).ToListAsync();

            return bookings.Select(b =>
            {
                var deadline = b.CheckInDate.ToDateTime(TimeOnly.MaxValue);
                string phase = b.BookingStatus switch
                {
                    "CHECKED_IN" => b.CheckOutDate < today ? "Overdue checkout"
                        : b.CheckOutDate == today ? "Due out today" : "In house",
                    "PENDING" or "CONFIRMED" => today > b.CheckInDate ? "Overdue check-in"
                        : b.CheckInDate > today ? "Upcoming" : "Arriving today",
                    "CHECKED_OUT" => "Checked out",
                    "CANCELLED" => "Cancelled",
                    _ => b.BookingStatus
                };
                return new GuestStayDto
                {
                    BookingId = b.BookingId, CustomerId = b.CustomerId,
                    CustomerName = b.CustomerName, CustomerPhone = b.CustomerPhone,
                    CheckInDate = b.CheckInDate, CheckOutDate = b.CheckOutDate, CheckInDeadline = deadline,
                    Status = b.BookingStatus, Phase = phase,
                    PaidAmount = payments.Where(p => p.BookingId == b.BookingId).Sum(p => p.Amount),
                    Rooms = details.Where(d => d.BookingId == b.BookingId).Select(d => new StayRoomLineDto
                    {
                        RoomTypeName = d.TypeName, Quantity = d.Quantity, UnitPrice = d.UnitPrice,
                        AssignedRooms = assigned.Where(a => a.BookingDetailId == d.BookingDetailId)
                            .Select(a => a.RoomNumber).OrderBy(n => n).ToList()
                    }).ToList(),
                    Services = services.Where(sv => sv.BookingId == b.BookingId).Select(sv => new StayServiceLineDto
                    { ServiceName = sv.ServiceName, Quantity = sv.Quantity, UnitPrice = sv.UnitPrice }).ToList()
                };
            }).ToList();
        }

        public Task<bool> CheckInGuestAsync(CheckInDto dto) => throw new NotImplementedException();
        public Task<InvoiceSummaryDto> CheckOutGuestAsync(string stayId) => throw new NotImplementedException();
        public Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string keyword) => throw new NotImplementedException();
        public Task<CustomerDto> GetCustomerByIdAsync(string customerId) => throw new NotImplementedException();
        public Task<bool> CreateCustomerProfileAsync(CustomerCreateDto dto) => throw new NotImplementedException();
        public Task<bool> UpdateCustomerProfileAsync(CustomerUpdateDto dto) => throw new NotImplementedException();
    }
}
