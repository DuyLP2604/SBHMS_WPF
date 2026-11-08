using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Stay;
using SHBMS.Helper;
using SHBMS.Model;
using System.Data;

namespace SHBMS.Service.Implementation;

public class StayWorkflowService
{
    private readonly Func<Prn212SbhmsContext> _createContext;
    private readonly Func<DateTime> _clock;

    public StayWorkflowService(Func<Prn212SbhmsContext>? createContext = null, Func<DateTime>? clock = null)
    {
        _createContext = createContext ?? (() => new Prn212SbhmsContext());
        _clock = clock ?? (() => DateTime.Now);
    }

    private static async Task VerifyStaffAsync(Prn212SbhmsContext db)
    {
        var account = Session.CurrentAccount;
        if (account?.Role != "Staff" || string.IsNullOrWhiteSpace(account.EmployeeId)
            || !await db.Employees.AnyAsync(e => e.EmployeeId == account.EmployeeId
                && e.UserId == account.UserId && e.User.Role == "Staff"))
            throw new UnauthorizedAccessException("Only Staff can check guests in or out.");
    }

    private static DateTime CheckInDeadline(Booking booking)
        => booking.CheckInDeadline ?? booking.CheckInDeadlineOverride
            ?? booking.CheckInDate.ToDateTime(new TimeOnly(14, 15));

    private static void ValidateCheckIn(Booking booking, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        if (booking.IsDeleted || booking.BookingStatus != "CONFIRMED")
            throw new InvalidOperationException("Only a confirmed booking can check in. Refresh the list.");
        if (today < booking.CheckInDate)
            throw new InvalidOperationException("This booking has not reached its check-in date.");
        if (today >= booking.CheckOutDate)
            throw new InvalidOperationException("The booking's stay dates have expired.");
        if (now > CheckInDeadline(booking))
            throw new InvalidOperationException("The check-in deadline has passed. Cancel this booking as a no-show.");
        if (booking.BookingDetails.Count == 0)
            throw new InvalidOperationException("This booking has no room details.");
    }

    private static async Task<Booking> ReadBookingAsync(Prn212SbhmsContext db, string bookingId)
    {
        return await db.Bookings.Include(b => b.BookingDetails).ThenInclude(d => d.RoomAssignments)
            .Include(b => b.BookingServices).Include(b => b.Payments).Include(b => b.Invoices)
            .AsSplitQuery().SingleOrDefaultAsync(b => b.BookingId == bookingId)
            ?? throw new InvalidOperationException("Booking no longer exists.");
    }

    private static async Task<List<Room>> AvailableRoomsAsync(
        Prn212SbhmsContext db, Booking booking, DateOnly today)
    {
        var typeIds = booking.BookingDetails.Select(d => d.RoomTypeId).ToList();
        // Checked-in guests keep their rooms until checkout, including overdue stays.
        // Other reservations block only overlapping dates; checkout date is exclusive.
        var blocked = db.RoomAssignments.Where(a => a.BookingDetail.BookingId != booking.BookingId
            && !a.BookingDetail.Booking.IsDeleted
            && (a.BookingDetail.Booking.BookingStatus == "CHECKED_IN"
                || ((a.BookingDetail.Booking.BookingStatus == "PENDING"
                    || a.BookingDetail.Booking.BookingStatus == "CONFIRMED")
                    && a.BookingDetail.Booking.CheckInDate < booking.CheckOutDate
                    && a.BookingDetail.Booking.CheckOutDate > today)))
            .Select(a => a.RoomId);
        return await db.Rooms.AsNoTracking()
            .Where(r => r.Status == "ACTIVE" && typeIds.Contains(r.RoomTypeId) && !blocked.Contains(r.RoomId))
            .OrderBy(r => r.RoomNumber).ToListAsync();
    }

    public async Task<List<Room>> GetAvailableRoomsAsync(string bookingId)
    {
        using var db = _createContext();
        await VerifyStaffAsync(db);
        var booking = await ReadBookingAsync(db, bookingId);
        DateTime now = _clock();
        var today = DateOnly.FromDateTime(now);
        ValidateCheckIn(booking, now);
        return await AvailableRoomsAsync(db, booking, today);
    }

    public async Task CheckInAsync(string bookingId, List<RoomSelectionDto> selections)
    {
        using var db = _createContext();
        // Recheck availability and save all room assignments + status together.
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await VerifyStaffAsync(db);
        var booking = await ReadBookingAsync(db, bookingId);
        DateTime now = _clock();
        var today = DateOnly.FromDateTime(now);
        ValidateCheckIn(booking, now);

        if (selections.Count != booking.BookingDetails.Sum(d => d.Quantity)
            || selections.Any(s => string.IsNullOrWhiteSpace(s.RoomId)))
            throw new ArgumentException("Select a room for every row.");
        if (selections.Select(s => s.RoomId).Distinct().Count() != selections.Count)
            throw new ArgumentException("Each selected room must be different.");
        if (selections.Any(s => !booking.BookingDetails.Any(d => d.RoomTypeId == s.RoomTypeId)))
            throw new ArgumentException("A selected room type does not belong to this booking.");

        var available = await AvailableRoomsAsync(db, booking, today);
        foreach (var detail in booking.BookingDetails)
        {
            var selected = selections.Where(s => s.RoomTypeId == detail.RoomTypeId).ToList();
            if (selected.Count != detail.Quantity)
                throw new ArgumentException("Select the correct number of rooms for each room type.");
            if (selected.Any(s => !available.Any(r => r.RoomId == s.RoomId && r.RoomTypeId == detail.RoomTypeId)))
                throw new InvalidOperationException("A selected room is no longer available. Cancel and reopen the form.");
        }
        // Replace draft assignments only after the whole request is valid.
        foreach (var detail in booking.BookingDetails)
        {
            db.RoomAssignments.RemoveRange(detail.RoomAssignments);
            foreach (var selected in selections.Where(s => s.RoomTypeId == detail.RoomTypeId))
                db.RoomAssignments.Add(new RoomAssignment
                {
                    BookingDetailId = detail.BookingDetailId, RoomId = selected.RoomId,
                    EmployeeId = Session.CurrentAccount!.EmployeeId, AssignedAt = now
                });
        }
        booking.BookingStatus = "CHECKED_IN";
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task CancelNoShowAsync(string bookingId)
    {
        using var db = _createContext();
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await VerifyStaffAsync(db);
        var booking = await ReadBookingAsync(db, bookingId);
        if (booking.IsDeleted || booking.BookingStatus != "CONFIRMED")
            throw new InvalidOperationException("Only a confirmed booking can be cancelled as a no-show. Refresh the list.");
        if (_clock() <= CheckInDeadline(booking))
            throw new InvalidOperationException("The check-in deadline has not passed yet.");

        // Cancellation keeps the booking, payments and room assignments as history.
        booking.BookingStatus = "CANCELLED";
        booking.CancellationReason = "No-show: check-in deadline expired.";
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<string> CheckOutAsync(string bookingId, decimal quotedTotal, decimal quotedPaid, bool cashReceived)
    {
        using var db = _createContext();
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await VerifyStaffAsync(db);
        var booking = await ReadBookingAsync(db, bookingId);
        if (booking.IsDeleted || booking.BookingStatus != "CHECKED_IN")
            throw new InvalidOperationException("Only a checked-in booking can check out. Refresh the list.");
        if (booking.Invoices.Count > 0)
            throw new InvalidOperationException("This booking already has an invoice. Review it before checkout.");

        // Keep the booked number of nights and booked unit prices, even for early checkout.
        int nights = booking.CheckOutDate.DayNumber - booking.CheckInDate.DayNumber;
        decimal total = booking.BookingDetails.Sum(d => d.Quantity * d.UnitPrice * nights)
            + booking.BookingServices.Sum(s => s.Quantity * s.UnitPrice);
        decimal paid = booking.Payments.Sum(p => p.Amount);
        if (total != quotedTotal || paid != quotedPaid)
            throw new InvalidOperationException("Charges or payments changed. Refresh and review the balance again.");
        decimal remaining = total - paid;
        if (remaining > 0 && !cashReceived)
            throw new ArgumentException("Confirm that the remaining cash payment has been received.");

        DateTime now = _clock();
        if (remaining > 0)
        {
            if (!await db.Paymentmethods.AnyAsync(m => m.MethodId == "PT03"))
                throw new InvalidOperationException("Cash payment method PT03 is missing from the database.");
            db.Payments.Add(new Payment
            {
                PaymentId = EntityIds.Next(await db.Payments.Select(p => p.PaymentId).ToListAsync(), "PM", 4),
                BookingId = bookingId, PaymentTime = now, Amount = remaining, MethodId = "PT03",
                CollectedByEmployee = Session.CurrentAccount!.EmployeeId, Note = "Remaining balance collected at checkout."
            });
        }
        string invoiceId = EntityIds.Next(await db.Invoices.Select(i => i.InvoiceId).ToListAsync(), "HD", 4);
        db.Invoices.Add(new Invoice
        {
            InvoiceId = invoiceId, BookingId = bookingId, InvoiceDate = now,
            TotalAmount = total, EmployeeId = Session.CurrentAccount!.EmployeeId
        });
        // Keep assignments as history. Room occupancy is derived from CHECKED_IN status.
        booking.BookingStatus = "CHECKED_OUT";
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return invoiceId;
    }
}
