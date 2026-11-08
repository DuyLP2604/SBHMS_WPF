using Microsoft.EntityFrameworkCore;
using SHBMS.Helper;
using SHBMS.DTO.Service;
using SHBMS.Model;

namespace SHBMS.Service.Implementation;

// Shared checks for requests, availability and already-provided service charges.
public static class ServiceWorkflowRules
{
    public static IQueryable<GuestServiceRequest> QueryRequests(Prn212SbhmsContext db)
        => from r in db.ServiceRequests.AsNoTracking()
           join s in db.Services.AsNoTracking() on r.ServiceId equals s.ServiceId
           join b in db.Bookings.AsNoTracking() on r.BookingId equals b.BookingId
           join c in db.Customers.AsNoTracking() on b.CustomerId equals c.CustomerId
           where !b.IsDeleted
           orderby r.CreatedAt descending, r.RequestId descending
           select new GuestServiceRequest
           {
               Id = r.RequestId, BookingId = r.BookingId, ServiceId = r.ServiceId,
               CustomerId = b.CustomerId, CustomerName = c.FullName,
               ServiceName = s.ServiceName, Details = r.RequestDescription, Status = r.RequestStatus,
               Quantity = r.Quantity, UnitPrice = r.UnitPrice, BilledAt = r.BilledAt,
               CreatedAt = r.CreatedAt, ResolvedAt = r.ResolvedAt, HandledBy = r.HandledByEmployeeId ?? ""
           };

    public static async Task VerifyViewerAsync(Prn212SbhmsContext db)
    {
        var account = Session.CurrentAccount;
        if (account?.Role is not ("Staff" or "Manager")
            || !await db.Users.AnyAsync(u => u.UserId == account.UserId && u.Role == account.Role))
            throw new UnauthorizedAccessException("Only Staff and Manager can view services.");
    }

    public static async Task<string> VerifyStaffAsync(Prn212SbhmsContext db)
    {
        var account = Session.CurrentAccount;
        if (account?.Role != "Staff" || string.IsNullOrWhiteSpace(account.EmployeeId)
            || !await db.Employees.AnyAsync(e => e.EmployeeId == account.EmployeeId
                && e.UserId == account.UserId && e.User.Role == "Staff"))
            throw new UnauthorizedAccessException("Only Staff with a linked employee account can change services.");
        return account.EmployeeId;
    }

    public static async Task<Booking> ReadOpenBookingAsync(Prn212SbhmsContext db, string bookingId)
    {
        var booking = await db.Bookings.Include(b => b.Invoices)
            .SingleOrDefaultAsync(b => b.BookingId == bookingId);
        if (booking == null || booking.IsDeleted || booking.BookingStatus != "CHECKED_IN" || booking.Invoices.Any())
            throw new InvalidOperationException("Services can only be changed for a checked-in booking without an invoice.");
        return booking;
    }

    // A booking/service pair keeps its first agreed price, including subsequent requests.
    public static async Task<decimal> GetBookedPriceAsync(Prn212SbhmsContext db, string bookingId, SHBMS.Model.Service service)
    {
        var billed = await db.BookingServices.Where(o => o.BookingId == bookingId && o.ServiceId == service.ServiceId)
            .Select(o => (decimal?)o.UnitPrice).FirstOrDefaultAsync();
        if (billed.HasValue) return billed.Value;
        var requested = await db.ServiceRequests.Where(r => r.BookingId == bookingId && r.ServiceId == service.ServiceId
                && r.RequestStatus != "Rejected").OrderBy(r => r.RequestId)
            .Select(r => (decimal?)r.UnitPrice).FirstOrDefaultAsync();
        return requested ?? service.UnitPrice;
    }

    // NULL stock means not tracked. Positive amounts consume stock; negative amounts return it.
    public static void ChangeStock(SHBMS.Model.Service service, int amount)
    {
        if (amount > 0 && (!service.IsAvailable || service.StockQuantity < amount))
            throw new InvalidOperationException("This service is unavailable or does not have enough stock.");
        if (service.StockQuantity.HasValue)
        {
            service.StockQuantity = checked(service.StockQuantity.Value - amount);
            if (service.StockQuantity == 0) service.IsAvailable = false;
        }
    }
}
