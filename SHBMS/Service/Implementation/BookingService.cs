using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Booking;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Interface;
using System.Data;
using System.Net.Mail;
namespace SHBMS.Service.Implementation;
public class BookingService : IBookingService
{
    private readonly Func<Prn212SbhmsContext> _createContext;
    private readonly Func<DateTime> _clock;
    public BookingService(Func<Prn212SbhmsContext>? createContext = null, Func<DateTime>? clock = null)
    {
        _createContext = createContext ?? (() => new Prn212SbhmsContext());
        _clock = clock ?? (() => DateTime.Now);
    }
    private static void RequireBookingView()
    {
        if (!Session.CanViewBookings)
            throw new UnauthorizedAccessException("Only Staff and Manager can access bookings.");
    }
    private static async Task VerifyBookingViewerAsync(Prn212SbhmsContext db)
    {
        var account = Session.CurrentAccount!;
        if (!await db.Users.AnyAsync(u => u.UserId == account.UserId
            && (u.Role == "Staff" || u.Role == "Manager")))
            throw new UnauthorizedAccessException("Your database account cannot access bookings.");
    }
    private static void ValidateDates(DateOnly arrival, DateOnly departure, DateOnly today)
    {
        if (arrival < today) throw new ArgumentException("Check-in date cannot be in the past.");
        if (departure <= arrival) throw new ArgumentException("Check-out date must be after check-in date.");
    }

    public async Task<List<BookingCustomerDto>> SearchCustomersAsync(string phone)
    {
        RequireBookingView();
        string keyword = phone.Trim();
        if (keyword.Length == 0) throw new ArgumentException("Enter a phone number to search.");
        using var db = _createContext();
        await VerifyBookingViewerAsync(db);
        return await db.Customers.AsNoTracking().Where(c => c.Phone.Contains(keyword)).OrderBy(c => c.FullName)
            .Select(c => new BookingCustomerDto { CustomerId = c.CustomerId, FullName = c.FullName, Phone = c.Phone,
                IdentityNumber = c.IdentityNumber, Email = c.Email, Address = c.Address, NationalityName = c.NationalityName }).ToListAsync();
    }
    public async Task<List<AvailableRoomTypeDto>> GetAvailableRoomTypesAsync(DateOnly arrival, DateOnly departure)
    {
        RequireBookingView();
        if (!Session.CanViewRooms)
            throw new UnauthorizedAccessException("Only Staff and Manager can view available room types.");
        var today = DateOnly.FromDateTime(_clock());
        ValidateDates(arrival, departure, today);
        using var db = _createContext();
        await VerifyBookingViewerAsync(db);
        return await ReadAvailabilityAsync(db, arrival, departure, today);
    }
    private static async Task<List<AvailableRoomTypeDto>> ReadAvailabilityAsync(
        Prn212SbhmsContext db, DateOnly arrival, DateOnly departure, DateOnly today)
    {
        var types = await db.RoomTypes.AsNoTracking().OrderBy(t => t.TypeName).ToListAsync();
        var rooms = await db.Rooms.AsNoTracking().Where(r => r.Status == "ACTIVE").ToListAsync();
        var reservations = await db.BookingDetails.AsNoTracking().Include(d => d.Booking).Include(d => d.RoomAssignments)
            .Where(d => !d.Booking.IsDeleted && (d.Booking.BookingStatus == "PENDING"
                || d.Booking.BookingStatus == "CONFIRMED" || d.Booking.BookingStatus == "CHECKED_IN"))
            .AsSplitQuery().ToListAsync();
        var result = new List<AvailableRoomTypeDto>();
        foreach (var type in types)
        {
            var activeIds = rooms.Where(r => r.RoomTypeId == type.RoomTypeId).Select(r => r.RoomId).ToHashSet();
            var fixedRooms = new HashSet<string>();
            var allDemand = new List<RoomReservation>();
            var unassignedDemand = new List<RoomReservation>();
            foreach (var detail in reservations.Where(d => d.RoomTypeId == type.RoomTypeId))
            {
                var booking = detail.Booking;
                bool inHouse = booking.BookingStatus == "CHECKED_IN";
                var start = inHouse && booking.CheckInDate > today ? today : booking.CheckInDate;
                var end = inHouse && booking.CheckOutDate <= today ? DateOnly.MaxValue : booking.CheckOutDate;
                start = start < arrival ? arrival : start;
                end = end > departure ? departure : end;
                if (end <= start) continue;
                allDemand.Add(new RoomReservation(start, end, detail.Quantity, false));
                var assigned = detail.RoomAssignments.Where(a => activeIds.Contains(a.RoomId)).Select(a => a.RoomId).Distinct().ToList();
                fixedRooms.UnionWith(assigned);
                int unassigned = Math.Max(0, detail.Quantity - assigned.Count);
                if (unassigned > 0) unassignedDemand.Add(new RoomReservation(start, end, unassigned, false));
            }
            int pooledFree = activeIds.Count - RoomInventoryRules.PeakDemand(allDemand, today);
            // Respect existing room numbers: a new stay needs a room free for its full duration.
            // Unassigned bookings also reserve stock. This deliberately avoids promising room moves.
            int continuousFree = activeIds.Count - fixedRooms.Count - RoomInventoryRules.PeakDemand(unassignedDemand, today);
            result.Add(new AvailableRoomTypeDto { RoomTypeId = type.RoomTypeId, Name = type.TypeName,
                Capacity = type.Capacity, UnitPrice = type.Price,
                AvailableRooms = Math.Max(0, Math.Min(pooledFree, continuousFree)) });
        }
        return result.OrderBy(t => t.AvailableRooms == 0).ThenBy(t => t.Name).ToList();
    }

    public async Task<string> CreateBookingAsync(BookingCreateDto dto)
    {
        var account = Session.CurrentAccount;
        if (account?.Role != "Staff" || account.EmployeeId == null)
            throw new UnauthorizedAccessException("Only Staff can create bookings for customers.");
        DateTime now = _clock();
        var today = DateOnly.FromDateTime(now);
        ValidateDates(dto.CheckInDate, dto.CheckOutDate, today);
        bool existingCustomer = !string.IsNullOrWhiteSpace(dto.CustomerId);
        if (existingCustomer == (dto.NewCustomer != null))
            throw new ArgumentException("Select an existing customer or enter a new customer.");
        if (dto.Rooms.Count == 0 || dto.Rooms.Any(r => string.IsNullOrWhiteSpace(r.RoomTypeId) || r.Quantity <= 0))
            throw new ArgumentException("Add at least one room type with a positive quantity.");
        if (dto.Rooms.Select(r => r.RoomTypeId).Distinct().Count() != dto.Rooms.Count)
            throw new ArgumentException("A room type can only appear once in the booking.");
        if (dto.NewCustomer != null) ValidateCustomer(dto.NewCustomer);

        using var db = _createContext();
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.Employees.AnyAsync(e => e.EmployeeId == account.EmployeeId && e.UserId == account.UserId && e.User.Role == "Staff"))
            throw new UnauthorizedAccessException("Your current database account does not have Staff permission.");

        // Search results are a quote. Recheck both inventory and prices inside the transaction.
        var availability = await ReadAvailabilityAsync(db, dto.CheckInDate, dto.CheckOutDate, today);
        foreach (var requested in dto.Rooms)
        {
            var type = availability.FirstOrDefault(t => t.RoomTypeId == requested.RoomTypeId)
                ?? throw new InvalidOperationException("A room type no longer exists. Search rooms again.");
            if (requested.Quantity > type.AvailableRooms)
                throw new InvalidOperationException($"Not enough {type.Name} rooms remain. Search rooms again.");
            if (requested.QuotedUnitPrice != type.UnitPrice)
                throw new InvalidOperationException("Room prices have changed. Search rooms again and review the new total.");
        }
        Customer customer;
        if (existingCustomer)
        {
            customer = await db.Customers.FindAsync(dto.CustomerId!)
                ?? throw new InvalidOperationException("Customer no longer exists. Search for the customer again.");
        }
        else
        {
            var form = dto.NewCustomer!;
            string? identity = EntityIds.Optional(form.IdentityNumber);
            if (identity != null && await db.Customers.AnyAsync(c => c.IdentityNumber == identity))
                throw new InvalidOperationException("Identity number already exists. Select the existing customer.");
            customer = new Customer {
                CustomerId = EntityIds.Next(await db.Customers.Select(c => c.CustomerId).ToListAsync(), "CUS", 3),
                FullName = form.FullName.Trim(), Phone = form.Phone.Trim(), IdentityNumber = identity,
                Email = EntityIds.Optional(form.Email), Address = EntityIds.Optional(form.Address),
                NationalityName = EntityIds.Optional(form.NationalityName)
            };
        }
        var booking = new Booking {
            BookingId = EntityIds.Next(await db.Bookings.Select(b => b.BookingId).ToListAsync(), "BK", 4),
            Customer = customer, EmployeeId = account.EmployeeId, BookingDate = now,
            CheckInDate = dto.CheckInDate, CheckOutDate = dto.CheckOutDate, BookingStatus = "CONFIRMED",
            CheckInDeadlineOverride = dto.CheckInDate == today && now.TimeOfDay >= new TimeSpan(14, 15, 0)
                ? dto.CheckInDate.ToDateTime(new TimeOnly(23, 59, 59)) : null
        };
        foreach (var requested in dto.Rooms)
        {
            var type = availability.Single(t => t.RoomTypeId == requested.RoomTypeId);
            booking.BookingDetails.Add(new BookingDetail { RoomTypeId = type.RoomTypeId,
                Quantity = requested.Quantity, UnitPrice = type.UnitPrice });
        }
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return booking.BookingId;
    }

    public async Task<List<BookingDto>> GetAllBookingsAsync()
    {
        RequireBookingView();
        using var db = _createContext();
        await VerifyBookingViewerAsync(db);
        var bookings = await BookingQuery(db).OrderByDescending(b => b.BookingDate).ThenByDescending(b => b.BookingId).ToListAsync();
        return bookings.Select(ToDto).ToList();
    }
    public async Task<BookingDto> GetBookingByIdAsync(string bookingId)
    {
        RequireBookingView();
        using var db = _createContext();
        await VerifyBookingViewerAsync(db);
        var booking = await BookingQuery(db).FirstOrDefaultAsync(b => b.BookingId == bookingId)
            ?? throw new InvalidOperationException("Booking no longer exists. Refresh the list.");
        return ToDto(booking);
    }
    private static IQueryable<Booking> BookingQuery(Prn212SbhmsContext db)
        => db.Bookings.AsNoTracking().Where(b => !b.IsDeleted).Include(b => b.Customer).Include(b => b.Employee)
            .Include(b => b.BookingDetails).ThenInclude(d => d.RoomType)
            .Include(b => b.BookingDetails).ThenInclude(d => d.RoomAssignments).ThenInclude(a => a.Room)
            .Include(b => b.BookingServices).ThenInclude(s => s.Service)
            .Include(b => b.Payments).ThenInclude(p => p.Method).AsSplitQuery();
    private static BookingDto ToDto(Booking b)
    {
        int nights = b.CheckOutDate.DayNumber - b.CheckInDate.DayNumber;
        return new BookingDto {
            BookingId = b.BookingId, CustomerId = b.CustomerId, CustomerName = b.Customer.FullName,
            CustomerPhone = b.Customer.Phone, EmployeeId = b.EmployeeId, CreatedByEmployeeName = b.Employee.FullName,
            BookingDate = b.BookingDate, CheckInDate = b.CheckInDate, CheckOutDate = b.CheckOutDate,
            CheckInDeadline = b.CheckInDeadline ?? b.CheckInDeadlineOverride ?? b.CheckInDate.ToDateTime(new TimeOnly(14, 15)),
            Status = b.BookingStatus, CancellationReason = b.CancellationReason,
            Rooms = b.BookingDetails.Select(d => new BookingRoomLineDto { RoomTypeId = d.RoomTypeId,
                TypeName = d.RoomType.TypeName, Quantity = d.Quantity, UnitPrice = d.UnitPrice, NumberOfNights = nights,
                AssignedRooms = string.Join(", ", d.RoomAssignments.Select(a => a.Room.RoomNumber)) }).ToList(),
            Services = b.BookingServices.Select(s => new BookingServiceLineDto { ServiceName = s.Service.ServiceName,
                Quantity = s.Quantity, UnitPrice = s.UnitPrice }).ToList(),
            Payments = b.Payments.OrderBy(p => p.PaymentTime).Select(p => new BookingPaymentDto {
                PaymentTime = p.PaymentTime, MethodName = p.Method.MethodName, Amount = p.Amount }).ToList()
        };
    }
    private static void ValidateCustomer(NewBookingCustomerDto c)
    {
        if (string.IsNullOrWhiteSpace(c.FullName) || string.IsNullOrWhiteSpace(c.Phone))
            throw new ArgumentException("Customer name and phone are required.");
        if (c.FullName.Trim().Length > 100 || c.Phone.Trim().Length > 15 || (c.IdentityNumber?.Trim().Length ?? 0) > 20
            || (c.Email?.Trim().Length ?? 0) > 100 || (c.Address?.Trim().Length ?? 0) > 200 || (c.NationalityName?.Trim().Length ?? 0) > 50)
            throw new ArgumentException("A customer field exceeds its allowed length.");
        if (!string.IsNullOrWhiteSpace(c.Email) && (!MailAddress.TryCreate(c.Email.Trim(), out var mail) || mail.Address != c.Email.Trim()))
            throw new ArgumentException("Invalid customer email address.");
    }
}
