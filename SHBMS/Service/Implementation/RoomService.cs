using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Room;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Interface;
using System.Data;

namespace SHBMS.Service.Implementation;

public class RoomService : IRoomService
{
    // A context is short-lived so tracked data is not shared between operations.
    private readonly Func<Prn212SbhmsContext> _createContext;

    /// <summary>
    /// Use a fresh database context for each room operation.
    /// </summary>
    public RoomService(Func<Prn212SbhmsContext>? createContext = null)
        => _createContext = createContext ?? (() => new Prn212SbhmsContext());

    /// <summary>
    /// Check that the session allows room and room type viewing.
    /// </summary>
    private static void RequireRoomView()
    {
        if (!Session.CanViewRooms)
            throw new UnauthorizedAccessException("Only Staff and Manager can view rooms and room types.");
    }

    /// <summary>
    /// Check the viewing role again against the current database account.
    /// </summary>
    private static async Task VerifyRoomViewerAsync(Prn212SbhmsContext db)
    {
        var account = Session.CurrentAccount!;
        if (!await db.Users.AnyAsync(u => u.UserId == account.UserId
                && (u.Role == "Staff" || u.Role == "Manager")))
            throw new UnauthorizedAccessException("Your database account cannot view rooms and room types.");
    }

    /// <summary>
    /// Require a Manager account linked to an employee before changing rooms.
    /// </summary>
    private static void RequireManager()
    {
        if (!Session.IsManager || Session.CurrentAccount?.EmployeeId == null)
            throw new UnauthorizedAccessException("Only Manager can add, update or delete rooms and room types.");
    }

    /// <summary>
    /// Verify the Manager and employee link in the database.
    /// </summary>
    private static async Task VerifyManagerAsync(Prn212SbhmsContext db)
    {
        var account = Session.CurrentAccount!;
        if (!await db.Employees.AnyAsync(e => e.EmployeeId == account.EmployeeId
                && e.UserId == account.UserId && e.User.Role == "Manager"))
            throw new UnauthorizedAccessException("Your current database account does not have Manager permission.");
    }

    /// <summary>
    /// Run a Manager change and its validation in one database transaction.
    /// </summary>
    private async Task<bool> WriteAsync(Func<Prn212SbhmsContext, Task> action)
    {
        RequireManager();
        using var db = _createContext();
        // Serializable isolation keeps inventory checks and the room change consistent.
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await VerifyManagerAsync(db);
        await action(db);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    /// <summary>
    /// Load room details and calculate occupancy from checked-in bookings.
    /// </summary>
    public async Task<IEnumerable<RoomDto>> GetAllRoomsAsync()
    {
        RequireRoomView();
        using var db = _createContext();
        await VerifyRoomViewerAsync(db);
        var rooms = await db.Rooms.AsNoTracking().Include(r => r.RoomType).OrderBy(r => r.RoomNumber).ToListAsync();
        // Occupied is calculated from live stays; it is not a manually edited room status.
        var stays = await db.RoomAssignments.AsNoTracking()
            .Where(a => !a.BookingDetail.Booking.IsDeleted && a.BookingDetail.Booking.BookingStatus == "CHECKED_IN")
            .Select(a => new
            {
                a.RoomId,
                a.BookingDetail.Booking.Customer.FullName
            }).ToListAsync();
        var guests = stays
            .GroupBy(s => s.RoomId)
            .ToDictionary(
            g => g.Key,
            g => string.Join(
                ", ",
                g
                .Select(s => s.FullName)
                .Distinct()
            )
        );
        return rooms.Select(r => new RoomDto
        {
            RoomId = r.RoomId,
            RoomNumber = r.RoomNumber,
            RoomTypeId = r.RoomTypeId,
            RoomTypeName = r.RoomType.TypeName,
            BasePrice = r.RoomType.Price,
            Status = ParseStatus(r.Status),
            RoomImage = r.RoomImage,
            IsOccupied = guests.ContainsKey(r.RoomId),
            CurrentGuestName = guests.GetValueOrDefault(r.RoomId, "")
        }).ToList();
    }

    /// <summary>
    /// Load room types with room counts and booking history flags.
    /// </summary>
    public async Task<IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync()
    {
        RequireRoomView();
        using var db = _createContext();
        await VerifyRoomViewerAsync(db);
        return await db.RoomTypes.AsNoTracking().OrderBy(r => r.TypeName).Select(r => new RoomTypeDto
        {
            RoomTypeId = r.RoomTypeId,
            Name = r.TypeName,
            Description = r.Description ?? "",
            BasePrice = r.Price,
            Capacity = r.Capacity,
            RoomTypeImage = r.RoomTypeImage,
            RoomCount = r.Rooms.Count(),
            HasBookingHistory = r.BookingDetails.Any()
        }).ToListAsync();
    }

    /// <summary>
    /// Return a smaller room status list for display.
    /// </summary>
    public async Task<IEnumerable<RoomStatusDto>> GetRealTimeRoomStatusesAsync()
        => (await GetAllRoomsAsync()).Select(r => new RoomStatusDto
        {
            RoomId = r.RoomId,
            RoomNumber = r.RoomNumber,
            RoomTypeName = r.RoomTypeName,
            Status = r.Status,
            CurrentGuestName = r.CurrentGuestName
        }).ToList();

    /// <summary>
    /// Validate and add a room with an automatically generated ID.
    /// </summary>
    public Task<bool> CreateRoomAsync(RoomCreateDto dto) => WriteAsync(async db =>
    {
        ValidateRoom(dto);
        await CheckRoomInputsAsync(db, dto, null);
        db.Rooms.Add(new SHBMS.Model.Room
        {
            RoomId = EntityIds.Next(await db.Rooms.Select(r => r.RoomId).ToListAsync(), "R", 2),
            RoomNumber = dto.RoomNumber.Trim(),
            RoomTypeId = dto.RoomTypeId,
            Status = StatusCode(dto.Status),
            RoomImage = EntityIds.Optional(dto.RoomImage)
        });
    });

    /// <summary>
    /// Update a room after checking assignments and available stock.
    /// </summary>
    public Task<bool> UpdateRoomAsync(RoomUpdateDto dto) => WriteAsync(async db =>
    {
        ValidateRoom(dto);
        var room = await db.Rooms.FindAsync(dto.RoomId)
            ?? throw new InvalidOperationException("Room no longer exists. Refresh the list.");
        await CheckRoomInputsAsync(db, dto, room.RoomId);
        await CheckRoomChangeAsync(db, room, dto.RoomTypeId, dto.Status);
        room.RoomNumber = dto.RoomNumber.Trim();
        room.RoomTypeId = dto.RoomTypeId;
        room.Status = StatusCode(dto.Status);
        room.RoomImage = EntityIds.Optional(dto.RoomImage);
    });

    /// <summary>
    /// Change operational status without breaking existing reservations.
    /// </summary>
    public Task<bool> UpdateRoomStatusAsync(string roomId, RoomStatusEnum newStatus) => WriteAsync(async db =>
    {
        _ = StatusCode(newStatus);
        var room = await db.Rooms.FindAsync(roomId)
            ?? throw new InvalidOperationException("Room no longer exists. Refresh the list.");
        await CheckRoomChangeAsync(db, room, room.RoomTypeId, newStatus);
        room.Status = StatusCode(newStatus);
    });

    /// <summary>
    /// Delete an unused room only when existing bookings remain supported.
    /// </summary>
    public Task<bool> DeleteRoomAsync(string roomId) => WriteAsync(async db =>
    {
        var room = await db.Rooms.FindAsync(roomId)
            ?? throw new InvalidOperationException("Room no longer exists. Refresh the list.");
        // Assignment history must remain readable after a guest checks out.
        if (await db.RoomAssignments.AnyAsync(a => a.RoomId == roomId))
            throw new InvalidOperationException("This room has assignment history and cannot be deleted. Use Inactive when appropriate.");
        if (room.Status == "ACTIVE")
            await CheckInventoryReductionAsync(db, room.RoomTypeId);
        db.Rooms.Remove(room);
    });

    /// <summary>
    /// Create a room type using an unused ID from RT01 to RT04.
    /// </summary>
    public Task<bool> CreateRoomTypeAsync(RoomTypeCreateDto dto) => WriteAsync(async db =>
    {
        ValidateRoomType(dto);
        await CheckTypeNameAsync(db, dto.Name, null);
        var existingIds = await db.RoomTypes.Select(r => r.RoomTypeId).ToListAsync();
        // Reuse a free ID within the four allowed types rather than creating RT05.
        string roomTypeId = new[]
        {
            "RT01",
            "RT02",
            "RT03",
            "RT04"
        }
            .FirstOrDefault(id => !existingIds.Contains(id))
            ?? throw new InvalidOperationException("Only four room types are used: RT01 to RT04. Select an existing type to update it.");
        db.RoomTypes.Add(new RoomType
        {
            RoomTypeId = roomTypeId,
            TypeName = dto.Name.Trim(),
            Capacity = dto.Capacity,
            Price = dto.BasePrice,
            Description = EntityIds.Optional(dto.Description),
            RoomTypeImage = EntityIds.Optional(dto.RoomTypeImage)
        });
    });

    /// <summary>
    /// Update room type details while protecting booked capacity and prices.
    /// </summary>
    public Task<bool> UpdateRoomTypeAsync(RoomTypeUpdateDto dto) => WriteAsync(async db =>
    {
        ValidateRoomType(dto);
        var type = await db.RoomTypes.FindAsync(dto.RoomTypeId)
            ?? throw new InvalidOperationException("Room type no longer exists. Refresh the list.");
        await CheckTypeNameAsync(db, dto.Name, type.RoomTypeId);
        // Pending bookings also reserve capacity, matching the database rule.
        if (dto.Capacity < type.Capacity && await db.BookingDetails.AnyAsync(d => d.RoomTypeId == type.RoomTypeId
                && !d.Booking.IsDeleted && (d.Booking.BookingStatus == "PENDING"
                || d.Booking.BookingStatus == "CONFIRMED" || d.Booking.BookingStatus == "CHECKED_IN")))
            throw new InvalidOperationException("Capacity cannot be reduced while this type has unfinished bookings.");
        type.TypeName = dto.Name.Trim();
        type.Capacity = dto.Capacity;
        type.Price = dto.BasePrice;
        type.Description = EntityIds.Optional(dto.Description);
        type.RoomTypeImage = EntityIds.Optional(dto.RoomTypeImage);
        // Existing bookings keep their saved price even when the room type price changes.
    });

    /// <summary>
    /// Delete a room type only when it has no rooms or booking history.
    /// </summary>
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
            // Use the existing stored procedure when the database is SQL Server.
            await db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.usp_DeleteRoomType @RoomTypeID={roomTypeId}, @EmployeeID={employeeId}");
        }
        else
            db.RoomTypes.Remove(type);
    });

    /// <summary>
    /// Reject duplicate room numbers and room types that no longer exist.
    /// </summary>
    private static async Task CheckRoomInputsAsync(Prn212SbhmsContext db, RoomCreateDto dto, string? exceptId)
    {
        string number = dto.RoomNumber.Trim();
        if (await db.Rooms.AnyAsync(r => r.RoomNumber == number && r.RoomId != exceptId))
            throw new InvalidOperationException("Room number already exists.");
        if (!await db.RoomTypes.AnyAsync(r => r.RoomTypeId == dto.RoomTypeId))
            throw new InvalidOperationException("Select an existing room type. Refresh the list if it was deleted.");
    }

    /// <summary>
    /// Require a unique room type name, excluding the current record.
    /// </summary>
    private static async Task CheckTypeNameAsync(Prn212SbhmsContext db, string name, string? exceptId)
    {
        string value = name.Trim();
        if (await db.RoomTypes.AnyAsync(r => r.TypeName == value && r.RoomTypeId != exceptId))
            throw new InvalidOperationException("Room type name already exists.");
    }

    /// <summary>
    /// Protect assigned stays before changing a room type or availability.
    /// </summary>
    private static async Task CheckRoomChangeAsync(
        Prn212SbhmsContext db,
        SHBMS.Model.Room room,
        string typeId,
        RoomStatusEnum status
    )
    {
        bool changesType = room.RoomTypeId != typeId;
        // Moving an ACTIVE room to another type also removes one room from the old type.
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
        if (removesAvailability)
            await CheckInventoryReductionAsync(db, room.RoomTypeId);
    }

    /// <summary>
    /// Ensure the remaining active rooms can cover existing booking demand.
    /// </summary>
    private static async Task CheckInventoryReductionAsync(Prn212SbhmsContext db, string typeId)
    {
        // Subtract the room being removed, including reservations without room assignments.
        int remaining = await db.Rooms.CountAsync(r => r.RoomTypeId == typeId && r.Status == "ACTIVE") - 1;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var details = await db.BookingDetails.AsNoTracking()
            .Where(d => d.RoomTypeId == typeId && !d.Booking.IsDeleted
                && (d.Booking.BookingStatus == "CHECKED_IN"
                || ((d.Booking.BookingStatus == "PENDING" || d.Booking.BookingStatus == "CONFIRMED") && d.Booking.CheckOutDate > today)))
            .Select(d => new
            {
                d.Booking.CheckInDate,
                d.Booking.CheckOutDate,
                d.Quantity,
                d.Booking.BookingStatus
            })
            .ToListAsync();
        // Use peak demand across stay dates, not the sum of every booking quantity.
        int needed = RoomInventoryRules.PeakDemand(details.Select(d => new RoomReservation(
                d.CheckInDate, d.CheckOutDate, d.Quantity, d.BookingStatus == "CHECKED_IN")), today);
        if (needed > remaining)
            throw new InvalidOperationException("This change would leave too few active rooms for existing bookings, including bookings not yet assigned a room.");
    }

    /// <summary>
    /// Check required room fields and database length limits.
    /// </summary>
    private static void ValidateRoom(RoomCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RoomNumber) || dto.RoomNumber.Trim().Length > 20)
            throw new ArgumentException("Room number is required and must not exceed 20 characters.");
        if (string.IsNullOrWhiteSpace(dto.RoomTypeId))
            throw new ArgumentException("Room type is required.");
        if ((dto.RoomImage?.Length ?? 0) > 100)
            throw new ArgumentException("Room image path must not exceed 100 characters.");
        _ = StatusCode(dto.Status);
    }

    /// <summary>
    /// Check room type values, price precision and database length limits.
    /// </summary>
    private static void ValidateRoomType(RoomTypeCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length > 50)
            throw new ArgumentException("Room type name is required and must not exceed 50 characters.");
        if (dto.Capacity <= 0)
            throw new ArgumentException("Capacity must be a positive integer.");
        if (dto.BasePrice <= 0 || dto.BasePrice > 9999999999.99m || decimal.Round(dto.BasePrice, 2) != dto.BasePrice)
            throw new ArgumentException("Price must be positive, at most 9,999,999,999.99 and have at most two decimal places.");
        if ((dto.Description?.Length ?? 0) > 500)
            throw new ArgumentException("Description must not exceed 500 characters.");
        if ((dto.RoomTypeImage?.Length ?? 0) > 255)
            throw new ArgumentException("Room type image path must not exceed 255 characters.");
    }

    /// <summary>
    /// Convert an editable operational status to its database value.
    /// </summary>
    private static string StatusCode(RoomStatusEnum status) => status switch
    {
        RoomStatusEnum.Active => "ACTIVE",
        RoomStatusEnum.Inactive => "INACTIVE",
        RoomStatusEnum.Maintenance => "MAINTENANCE",
        _ => throw new ArgumentException("Invalid room status.")
    };

    /// <summary>
    /// Convert a stored operational status to the value used by the form.
    /// </summary>
    private static RoomStatusEnum ParseStatus(string status) => status switch
    {
        "ACTIVE" => RoomStatusEnum.Active,
        "INACTIVE" => RoomStatusEnum.Inactive,
        "MAINTENANCE" => RoomStatusEnum.Maintenance,
        _ => throw new InvalidOperationException("Unknown room status in database.")
    };
}
