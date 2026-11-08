
using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Service;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation;

public class ServiceRequestManager : IServiceRequestManager
{
    private readonly Func<Prn212SbhmsContext> _createContext;

    public ServiceRequestManager(
        Func<Prn212SbhmsContext>? createContext = null)
    {
        _createContext = createContext
            ?? (() => new Prn212SbhmsContext());
    }

    // Get all requests
    public async Task<List<GuestServiceRequest>> GetRequestsAsync()
    {
        if (!Session.IsStaff)
            throw new UnauthorizedAccessException();

        using var db = _createContext();

        return await (
            from r in db.ServiceRequests.AsNoTracking()
            join s in db.Services.AsNoTracking()
                on r.ServiceId equals s.ServiceId
            orderby r.CreatedAt descending
            select new GuestServiceRequest
            {
                Id = r.RequestId,
                BookingId = r.BookingId,
                ServiceId = r.ServiceId,
                ServiceName = s.ServiceName,
                Details = r.RequestDescription,
                Status = r.RequestStatus,
                CreatedAt = r.CreatedAt,
                ResolvedAt = r.ResolvedAt,
                HandledBy = r.HandledByEmployeeId ?? ""
            }
        ).ToListAsync();
    }

    // Get checked-in bookings
    public async Task<List<Booking>> GetCheckedInBookingsAsync()
    {
        if (!Session.IsStaff)
            throw new UnauthorizedAccessException();

        using var db = _createContext();

        return await db.Bookings
            .AsNoTracking()
            .Where(b => !b.IsDeleted &&
                        b.BookingStatus == "CHECKED_IN")
            .OrderBy(b => b.BookingId)
            .ToListAsync();
    }

    // Get hotel services
    public async Task<List<SHBMS.Model.Service>> GetServicesAsync()
    {
        if (!Session.IsStaff)
            throw new UnauthorizedAccessException();

        using var db = _createContext();

        return await db.Services
            .AsNoTracking()
            .OrderBy(s => s.ServiceName)
            .ToListAsync();
    }

    // Create request
    public async Task AddAsync(GuestServiceRequest request)
    {
        if (!Session.IsStaff)
            throw new UnauthorizedAccessException();

        if (string.IsNullOrWhiteSpace(request.Details))
            throw new ArgumentException("Details cannot be empty.");

        if (request.Details.Trim().Length > 500)
            throw new ArgumentException(
                "Details cannot exceed 500 characters.");

        using var db = _createContext();

        bool bookingExists = await db.Bookings.AnyAsync(
            b => b.BookingId == request.BookingId &&
                 !b.IsDeleted &&
                 b.BookingStatus == "CHECKED_IN");

        if (!bookingExists)
            throw new InvalidOperationException(
                "Booking is not checked in.");

        bool serviceExists = await db.Services.AnyAsync(
            s => s.ServiceId == request.ServiceId);

        if (!serviceExists)
            throw new InvalidOperationException(
                "Service does not exist.");

        var entity = new ServiceRequest
        {
            BookingId = request.BookingId,
            ServiceId = request.ServiceId,
            RequestDescription = request.Details.Trim(),
            RequestStatus = "Pending",
            CreatedAt = DateTime.Now
        };

        db.ServiceRequests.Add(entity);
        await db.SaveChangesAsync();
    }

    // Update status
    public async Task<bool> UpdateStatusAsync(
        int id, string status, string staffId)
    {
        if (!Session.IsStaff)
            throw new UnauthorizedAccessException();

        string[] statuses =
        {
            "Pending",
            "InProgress",
            "Resolved",
            "Rejected"
        };

        if (!statuses.Contains(status))
            throw new ArgumentException("Invalid status.");

        if (string.IsNullOrWhiteSpace(staffId))
            throw new InvalidOperationException(
                "Staff ID is missing.");

        using var db = _createContext();

        bool validStaff = await db.Employees
            .AsNoTracking()
            .AnyAsync(e =>
                e.EmployeeId == staffId &&
                e.UserId == Session.CurrentAccount!.UserId);

        if (!validStaff)
            throw new UnauthorizedAccessException(
                "Invalid staff account.");

        var request = await db.ServiceRequests
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null)
            return false;

        if (request.RequestStatus is "Resolved" or "Rejected")
            throw new InvalidOperationException(
                "Request is already closed.");

        request.RequestStatus = status;
        request.HandledByEmployeeId = staffId;

        request.ResolvedAt =
            status is "Resolved" or "Rejected"
                ? DateTime.Now
                : null;

        await db.SaveChangesAsync();

        return true;
    }
}
