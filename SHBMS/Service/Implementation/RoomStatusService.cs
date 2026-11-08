using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Room;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Interface;
using System.Data;
namespace SHBMS.Service.Implementation;

// FE-07 Monitor Room Status. Reads ROOM, ROOM_ASSIGNMENT and BOOKING; updates only ROOM.Status.
// Room CRUD (FE-12) stays in RoomService; this class does not change it.
public class RoomStatusService : IRoomStatusService
{
    private readonly Func<Prn212SbhmsContext> _createContext;
    public RoomStatusService(Func<Prn212SbhmsContext>? createContext = null)
        => _createContext = createContext ?? (() => new Prn212SbhmsContext());

    private static void RequireView()
    {
        if (!Session.CanViewRooms)
            throw new UnauthorizedAccessException("Only Staff and Manager can view room status.");
    }
    private static async Task VerifyViewerAsync(Prn212SbhmsContext db)
    {
        var account = Session.CurrentAccount!;
        if (!await db.Users.AnyAsync(u => u.UserId == account.UserId && (u.Role == "Staff" || u.Role == "Manager")))
            throw new UnauthorizedAccessException("Your database account cannot view room status.");
    }
    private static void RequireStaff()
    {
        var account = Session.CurrentAccount;
        if (account?.Role != "Staff" || account.EmployeeId == null)
            throw new UnauthorizedAccessException("Only Staff can update room status.");
    }
    private static async Task VerifyStaffAsync(Prn212SbhmsContext db)
    {
        var account = Session.CurrentAccount!;
        if (!await db.Employees.AnyAsync(e => e.EmployeeId == account.EmployeeId
            && e.UserId == account.UserId && e.User.Role == "Staff"))
            throw new UnauthorizedAccessException("Your current database account does not have Staff permission.");
    }

    public async Task<List<RoomStatusDto>> GetRoomStatusesAsync()
    {
        RequireView();
        using var db = _createContext();
        await VerifyViewerAsync(db);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rooms = await db.Rooms.AsNoTracking().OrderBy(r => r.RoomNumber)
            .Select(r => new { r.RoomId, r.RoomNumber, r.Status, r.RoomTypeId, TypeName = r.RoomType.TypeName })
            .ToListAsync();
        // In-house guests, plus guests arriving today who already have a room assigned.
        var stays = await db.RoomAssignments.AsNoTracking()
            .Where(a => !a.BookingDetail.Booking.IsDeleted
                && (a.BookingDetail.Booking.BookingStatus == "CHECKED_IN"
                    || ((a.BookingDetail.Booking.BookingStatus == "PENDING" || a.BookingDetail.Booking.BookingStatus == "CONFIRMED")
                        && a.BookingDetail.Booking.CheckInDate <= today && a.BookingDetail.Booking.CheckOutDate > today)))
            .Select(a => new
            {
                a.RoomId, a.BookingDetail.BookingId, a.BookingDetail.Booking.BookingStatus,
                a.BookingDetail.Booking.CheckOutDate, a.BookingDetail.Booking.Customer.FullName
            }).ToListAsync();
        return rooms.Select(r =>
        {
            var inHouse = stays.Where(s => s.RoomId == r.RoomId && s.BookingStatus == "CHECKED_IN").ToList();
            var arriving = stays.Where(s => s.RoomId == r.RoomId && s.BookingStatus != "CHECKED_IN").ToList();
            var current = inHouse.FirstOrDefault();
            return new RoomStatusDto
            {
                RoomId = r.RoomId, RoomNumber = r.RoomNumber, RoomTypeId = r.RoomTypeId, RoomTypeName = r.TypeName,
                Status = ParseStatus(r.Status),
                IsOccupied = inHouse.Count > 0,
                CurrentGuestName = string.Join(", ", inHouse.Select(s => s.FullName).Distinct()),
                BookingId = current?.BookingId ?? arriving.FirstOrDefault()?.BookingId ?? "",
                ExpectedCheckOut = current?.CheckOutDate,
                IsReserved = inHouse.Count == 0 && arriving.Count > 0,
                ReservedGuestName = string.Join(", ", arriving.Select(s => s.FullName).Distinct())
            };
        }).ToList();
    }

    public async Task UpdateRoomStatusAsync(string roomId, RoomStatusEnum newStatus)
    {
        RequireStaff();
        string code = StatusCode(newStatus);
        using var db = _createContext();
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await VerifyStaffAsync(db);
        var room = await db.Rooms.FindAsync(roomId)
            ?? throw new InvalidOperationException("Room no longer exists. Refresh the list.");
        if (room.Status != code)
        {
            await EnsureNoCurrentOrUpcomingStayAsync(db, room.RoomId, code);
            // Taking an ACTIVE room out of service must leave enough rooms for existing bookings.
            if (room.Status == "ACTIVE") await EnsureInventoryAsync(db, room.RoomTypeId);
        }
        room.Status = code;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    // Same rule as the Rooms screen: a room tied to a stay cannot be made unavailable.
    private static async Task EnsureNoCurrentOrUpcomingStayAsync(Prn212SbhmsContext db, string roomId, string newCode)
    {
        if (newCode == "ACTIVE") return;
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (await db.RoomAssignments.AnyAsync(a => a.RoomId == roomId && !a.BookingDetail.Booking.IsDeleted
            && (a.BookingDetail.Booking.BookingStatus == "CHECKED_IN"
                || ((a.BookingDetail.Booking.BookingStatus == "PENDING" || a.BookingDetail.Booking.BookingStatus == "CONFIRMED")
                    && a.BookingDetail.Booking.CheckOutDate > today))))
            throw new InvalidOperationException("This room is assigned to a current or upcoming stay, so it cannot be set to Maintenance or Inactive.");
    }

    private static async Task EnsureInventoryAsync(Prn212SbhmsContext db, string typeId)
    {
        int remaining = await db.Rooms.CountAsync(r => r.RoomTypeId == typeId && r.Status == "ACTIVE") - 1;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var details = await db.BookingDetails.AsNoTracking()
            .Where(d => d.RoomTypeId == typeId && !d.Booking.IsDeleted
                && (d.Booking.BookingStatus == "CHECKED_IN"
                    || ((d.Booking.BookingStatus == "PENDING" || d.Booking.BookingStatus == "CONFIRMED") && d.Booking.CheckOutDate > today)))
            .Select(d => new { d.Booking.CheckInDate, d.Booking.CheckOutDate, d.Quantity, d.Booking.BookingStatus }).ToListAsync();
        int needed = RoomInventoryRules.PeakDemand(details.Select(d => new RoomReservation(
            d.CheckInDate, d.CheckOutDate, d.Quantity, d.BookingStatus == "CHECKED_IN")), today);
        if (needed > remaining)
            throw new InvalidOperationException("This change would leave too few active rooms for existing bookings, including bookings not yet assigned a room.");
    }

    private static string StatusCode(RoomStatusEnum status) => status switch
    {
        RoomStatusEnum.Active => "ACTIVE", RoomStatusEnum.Inactive => "INACTIVE",
        RoomStatusEnum.Maintenance => "MAINTENANCE", _ => throw new ArgumentException("Invalid room status.")
    };
    private static RoomStatusEnum ParseStatus(string status) => status switch
    {
        "ACTIVE" => RoomStatusEnum.Active, "INACTIVE" => RoomStatusEnum.Inactive,
        "MAINTENANCE" => RoomStatusEnum.Maintenance, _ => throw new InvalidOperationException("Unknown room status in database.")
    };
}
