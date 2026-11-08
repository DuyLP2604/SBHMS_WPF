using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Room;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Interface;
using System.Data;
namespace SHBMS.Service.Implementation;
public class RoomService : IRoomService
{
    private readonly Func<Prn212SbhmsContext> _createContext;
    public RoomService(Func<Prn212SbhmsContext>? createContext = null)
        => _createContext = createContext ?? (() => new Prn212SbhmsContext());

    private static void RequireRoomView()
    {
        if (!Session.CanViewRooms)
            throw new UnauthorizedAccessException("Only Staff and Manager can view rooms and room types.");
    }
    private static async Task VerifyRoomViewerAsync(Prn212SbhmsContext db)
    {
        var account = Session.CurrentAccount!;
        if (!await db.Users.AnyAsync(u => u.UserId == account.UserId
            && (u.Role == "Staff" || u.Role == "Manager")))
            throw new UnauthorizedAccessException("Your database account cannot view rooms and room types.");
    }
    private static void RequireManager()
    {
        if (!Session.IsManager || Session.CurrentAccount?.EmployeeId == null)
            throw new UnauthorizedAccessException("Only Manager can add, update or delete rooms and room types.");
    }
    private static async Task VerifyManagerAsync(Prn212SbhmsContext db)
    {
        var account = Session.CurrentAccount!;
        if (!await db.Employees.AnyAsync(e => e.EmployeeId == account.EmployeeId
            && e.UserId == account.UserId && e.User.Role == "Manager"))
            throw new UnauthorizedAccessException("Your current database account does not have Manager permission.");
    }
    private async Task<bool> WriteAsync(Func<Prn212SbhmsContext, Task> action)
    {
        RequireManager();
        using var db = _createContext();
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await VerifyManagerAsync(db);
        await action(db);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<IEnumerable<RoomDto>> GetAllRoomsAsync()
    {
        RequireRoomView();
        using var db = _createContext();
        await VerifyRoomViewerAsync(db);
        var rooms = await db.Rooms.AsNoTracking().Include(r => r.RoomType).OrderBy(r => r.RoomNumber).ToListAsync();
        var stays = await db.RoomAssignments.AsNoTracking()
            .Where(a => !a.BookingDetail.Booking.IsDeleted && a.BookingDetail.Booking.BookingStatus == "CHECKED_IN")
            .Select(a => new { a.RoomId, a.BookingDetail.Booking.Customer.FullName }).ToListAsync();
        var guests = stays.GroupBy(s => s.RoomId).ToDictionary(g => g.Key, g => string.Join(", ", g.Select(s => s.FullName).Distinct()));
        return rooms.Select(r => new RoomDto
        {
            RoomId = r.RoomId, RoomNumber = r.RoomNumber, RoomTypeId = r.RoomTypeId,
            RoomTypeName = r.RoomType.TypeName, BasePrice = r.RoomType.Price,
            Status = ParseStatus(r.Status), RoomImage = r.RoomImage,
            IsOccupied = guests.ContainsKey(r.RoomId), CurrentGuestName = guests.GetValueOrDefault(r.RoomId, "")
        }).ToList();
    }
    public async Task<IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync()
    {
        RequireRoomView();
        using var db = _createContext();
        await VerifyRoomViewerAsync(db);
        return await db.RoomTypes.AsNoTracking().OrderBy(r => r.TypeName).Select(r => new RoomTypeDto
        {
            RoomTypeId = r.RoomTypeId, Name = r.TypeName, Description = r.Description ?? "",
            BasePrice = r.Price, Capacity = r.Capacity, RoomTypeImage = r.RoomTypeImage,
            RoomCount = r.Rooms.Count(), HasBookingHistory = r.BookingDetails.Any()
        }).ToListAsync();
    }
    public async Task<IEnumerable<RoomStatusDto>> GetRealTimeRoomStatusesAsync()
        => (await GetAllRoomsAsync()).Select(r => new RoomStatusDto
        {
            RoomId = r.RoomId, RoomNumber = r.RoomNumber, RoomTypeName = r.RoomTypeName,
            Status = r.Status, CurrentGuestName = r.CurrentGuestName
        }).ToList();

    public Task<bool> CreateRoomAsync(RoomCreateDto dto) => WriteAsync(async db =>
    {
        ValidateRoom(dto);
        await CheckRoomInputsAsync(db, dto, null);
        db.Rooms.Add(new SHBMS.Model.Room
        {
            RoomId = EntityIds.Next(await db.Rooms.Select(r => r.RoomId).ToListAsync(), "R", 2),
            RoomNumber = dto.RoomNumber.Trim(), RoomTypeId = dto.RoomTypeId,
            Status = StatusCode(dto.Status), RoomImage = EntityIds.Optional(dto.RoomImage)
        });
    });
    public Task<bool> UpdateRoomAsync(RoomUpdateDto dto) => WriteAsync(async db =>
    {
        ValidateRoom(dto);
        var room = await db.Rooms.FindAsync(dto.RoomId)
            ?? throw new InvalidOperationException("Room no longer exists. Refresh the list.");
        await CheckRoomInputsAsync(db, dto, room.RoomId);
        await CheckRoomChangeAsync(db, room, dto.RoomTypeId, dto.Status);
        room.RoomNumber = dto.RoomNumber.Trim(); room.RoomTypeId = dto.RoomTypeId;
        room.Status = StatusCode(dto.Status); room.RoomImage = EntityIds.Optional(dto.RoomImage);
    });
    public Task<bool> UpdateRoomStatusAsync(string roomId, RoomStatusEnum newStatus) => WriteAsync(async db =>
    {
        _ = StatusCode(newStatus);
        var room = await db.Rooms.FindAsync(roomId)
            ?? throw new InvalidOperationException("Room no longer exists. Refresh the list.");
        await CheckRoomChangeAsync(db, room, room.RoomTypeId, newStatus);
        room.Status = StatusCode(newStatus);
    });
    public Task<bool> DeleteRoomAsync(string roomId) => WriteAsync(async db =>
    {
        var room = await db.Rooms.FindAsync(roomId)
            ?? throw new InvalidOperationException("Room no longer exists. Refresh the list.");
        if (await db.RoomAssignments.AnyAsync(a => a.RoomId == roomId))
            throw new InvalidOperationException("This room has assignment history and cannot be deleted. Use Inactive when appropriate.");
        if (room.Status == "ACTIVE") await CheckInventoryReductionAsync(db, room.RoomTypeId);
        db.Rooms.Remove(room);
    });

    public Task<bool> CreateRoomTypeAsync(RoomTypeCreateDto dto) => WriteAsync(async db =>
    {
        ValidateRoomType(dto);
        await CheckTypeNameAsync(db, dto.Name, null);
        db.RoomTypes.Add(new RoomType
        {
            RoomTypeId = EntityIds.Next(await db.RoomTypes.Select(r => r.RoomTypeId).ToListAsync(), "RT", 2),
            TypeName = dto.Name.Trim(), Capacity = dto.Capacity, Price = dto.BasePrice,
            Description = EntityIds.Optional(dto.Description), RoomTypeImage = EntityIds.Optional(dto.RoomTypeImage)
        });
    });
    public Task<bool> UpdateRoomTypeAsync(RoomTypeUpdateDto dto) => WriteAsync(async db =>
    {
        ValidateRoomType(dto);
        var type = await db.RoomTypes.FindAsync(dto.RoomTypeId)
            ?? throw new InvalidOperationException("Room type no longer exists. Refresh the list.");
        await CheckTypeNameAsync(db, dto.Name, type.RoomTypeId);
        // Match the existing SQL Server trigger, including pending bookings.
        if (dto.Capacity < type.Capacity && await db.BookingDetails.AnyAsync(d => d.RoomTypeId == type.RoomTypeId
            && !d.Booking.IsDeleted && (d.Booking.BookingStatus == "PENDING"
                || d.Booking.BookingStatus == "CONFIRMED" || d.Booking.BookingStatus == "CHECKED_IN")))
            throw new InvalidOperationException("Capacity cannot be reduced while this type has unfinished bookings.");
        type.TypeName = dto.Name.Trim(); type.Capacity = dto.Capacity;
        type.Price = dto.BasePrice; type.Description = EntityIds.Optional(dto.Description);
        type.RoomTypeImage = EntityIds.Optional(dto.RoomTypeImage);
        // BookingDetail.UnitPrice is a historical snapshot; never update it here.
    });
    public Task<bool> DeleteRoomTypeAsync(string roomTypeId) => WriteAsync(async db =>
    {
        var type = await db.RoomTypes.FindAsync(roomTypeId)
            ?? throw new InvalidOperationException("Room type no longer exists. Refresh the list.");
        if (await db.Rooms.AnyAsync(r => r.RoomTypeId == roomTypeId))
            throw new InvalidOperationException("This room type still contains rooms and cannot be deleted.");
        if (await db.BookingDetails.AnyAsync(d => d.RoomTypeId == roomTypeId))
            throw new InvalidOperationException("This room type has booking history and cannot be deleted.");
        if (db.Database.IsSqlServer())
        {
            string employeeId = Session.CurrentAccount!.EmployeeId!;
            await db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.usp_DeleteRoomType @RoomTypeID={roomTypeId}, @EmployeeID={employeeId}");
        }
        else db.RoomTypes.Remove(type);
    });

    private static async Task CheckRoomInputsAsync(Prn212SbhmsContext db, RoomCreateDto dto, string? exceptId)
    {
        string number = dto.RoomNumber.Trim();
        if (await db.Rooms.AnyAsync(r => r.RoomNumber == number && r.RoomId != exceptId))
            throw new InvalidOperationException("Room number already exists.");
        if (!await db.RoomTypes.AnyAsync(r => r.RoomTypeId == dto.RoomTypeId))
            throw new InvalidOperationException("Select an existing room type. Refresh the list if it was deleted.");
    }
    private static async Task CheckTypeNameAsync(Prn212SbhmsContext db, string name, string? exceptId)
    {
        string value = name.Trim();
        if (await db.RoomTypes.AnyAsync(r => r.TypeName == value && r.RoomTypeId != exceptId))
            throw new InvalidOperationException("Room type name already exists.");
    }
    private static async Task CheckRoomChangeAsync(Prn212SbhmsContext db, SHBMS.Model.Room room, string typeId, RoomStatusEnum status)
    {
        bool changesType = room.RoomTypeId != typeId;
        bool removesAvailability = room.Status == "ACTIVE" && (changesType || status != RoomStatusEnum.Active);
        if (changesType || (room.Status != StatusCode(status) && status != RoomStatusEnum.Active))
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (await db.RoomAssignments.AnyAsync(a => a.RoomId == room.RoomId && !a.BookingDetail.Booking.IsDeleted
                && (a.BookingDetail.Booking.BookingStatus == "CHECKED_IN"
                    || ((a.BookingDetail.Booking.BookingStatus == "PENDING" || a.BookingDetail.Booking.BookingStatus == "CONFIRMED")
                        && a.BookingDetail.Booking.CheckOutDate > today))))
                throw new InvalidOperationException("This room is assigned to a current or upcoming stay. Its type and availability cannot be changed.");
        }
        if (removesAvailability) await CheckInventoryReductionAsync(db, room.RoomTypeId);
    }
    private static async Task CheckInventoryReductionAsync(Prn212SbhmsContext db, string typeId)
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
    private static void ValidateRoom(RoomCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RoomNumber) || dto.RoomNumber.Trim().Length > 20)
            throw new ArgumentException("Room number is required and must not exceed 20 characters.");
        if (string.IsNullOrWhiteSpace(dto.RoomTypeId)) throw new ArgumentException("Room type is required.");
        if ((dto.RoomImage?.Length ?? 0) > 100) throw new ArgumentException("Room image path must not exceed 100 characters.");
        _ = StatusCode(dto.Status);
    }
    private static void ValidateRoomType(RoomTypeCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length > 50)
            throw new ArgumentException("Room type name is required and must not exceed 50 characters.");
        if (dto.Capacity <= 0) throw new ArgumentException("Capacity must be a positive integer.");
        if (dto.BasePrice <= 0 || dto.BasePrice > 9999999999.99m || decimal.Round(dto.BasePrice, 2) != dto.BasePrice)
            throw new ArgumentException("Price must be positive, at most 9,999,999,999.99 and have at most two decimal places.");
        if ((dto.Description?.Length ?? 0) > 500) throw new ArgumentException("Description must not exceed 500 characters.");
        if ((dto.RoomTypeImage?.Length ?? 0) > 255) throw new ArgumentException("Room type image path must not exceed 255 characters.");
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
