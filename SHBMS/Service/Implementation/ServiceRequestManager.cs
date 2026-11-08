using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Service;
using SHBMS.Model;
using SHBMS.Service.Interface;
using System.Data;

namespace SHBMS.Service.Implementation;

public class ServiceRequestManager : IServiceRequestManager
{
    private readonly Func<Prn212SbhmsContext> _createContext;
    public ServiceRequestManager(Func<Prn212SbhmsContext>? createContext = null)
        => _createContext = createContext ?? (() => new Prn212SbhmsContext());

    public async Task<List<GuestServiceRequest>> GetRequestsAsync()
    {
        using var db = _createContext();
        await ServiceWorkflowRules.VerifyStaffAsync(db);
        return await ServiceWorkflowRules.QueryRequests(db).ToListAsync();
    }

    public async Task<List<Booking>> GetCheckedInBookingsAsync()
    {
        using var db = _createContext();
        await ServiceWorkflowRules.VerifyStaffAsync(db);
        return await db.Bookings.AsNoTracking().Where(b => !b.IsDeleted && b.BookingStatus == "CHECKED_IN"
            && !b.Invoices.Any()).OrderBy(b => b.BookingId).ToListAsync();
    }

    public async Task<List<SHBMS.Model.Service>> GetServicesAsync()
    {
        using var db = _createContext();
        await ServiceWorkflowRules.VerifyStaffAsync(db);
        return await db.Services.AsNoTracking().Where(s => s.IsAvailable
            && (s.StockQuantity == null || s.StockQuantity > 0)).OrderBy(s => s.ServiceName).ToListAsync();
    }

    public async Task AddAsync(GuestServiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Details) || request.Details.Trim().Length > 500)
            throw new ArgumentException("Enter request details using 1 to 500 characters.");
        if (request.Quantity < 1) throw new ArgumentException("Quantity must be a positive whole number.");
        using var db = _createContext();
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ServiceWorkflowRules.VerifyStaffAsync(db);
        await ServiceWorkflowRules.ReadOpenBookingAsync(db, request.BookingId);
        var service = await db.Services.SingleOrDefaultAsync(s => s.ServiceId == request.ServiceId)
            ?? throw new InvalidOperationException("Service no longer exists.");
        if (!service.IsAvailable || service.StockQuantity < request.Quantity)
            throw new InvalidOperationException("This service is unavailable or does not have enough stock.");
        db.ServiceRequests.Add(new ServiceRequest
        {
            BookingId = request.BookingId, ServiceId = request.ServiceId,
            RequestDescription = request.Details.Trim(), Quantity = request.Quantity,
            UnitPrice = await ServiceWorkflowRules.GetBookedPriceAsync(db, request.BookingId, service),
            RequestStatus = "Pending", CreatedAt = DateTime.Now
        });
        // Pending requests do not create charges or consume stock.
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, string staffId)
    {
        if (status is not ("Pending" or "InProgress" or "Resolved" or "Rejected"))
            throw new ArgumentException("Invalid request status.");
        using var db = _createContext();
        string employeeId = await ServiceWorkflowRules.VerifyStaffAsync(db);
        if (staffId != employeeId) throw new UnauthorizedAccessException("The collecting employee must match the signed-in staff account.");
        // Locate the immutable booking key before holding transactional locks.
        string? bookingId = await db.ServiceRequests.Where(r => r.RequestId == id).Select(r => r.BookingId).FirstOrDefaultAsync();
        if (bookingId == null) return false;
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ServiceWorkflowRules.ReadOpenBookingAsync(db, bookingId);
        var request = await db.ServiceRequests.SingleAsync(r => r.RequestId == id);
        if (request.RequestStatus is "Resolved" or "Rejected")
            throw new InvalidOperationException("This request is already closed. Its charge will not be recorded again.");
        if (request.RequestStatus == status) return true;
        if (status == "Pending") throw new InvalidOperationException("An in-progress request cannot be moved back to Pending.");
        DateTime now = DateTime.Now;
        if (status == "Resolved")
        {
            if (request.BilledAt != null) throw new InvalidOperationException("This request has already been billed.");
            var service = await db.Services.SingleAsync(s => s.ServiceId == request.ServiceId);
            ServiceWorkflowRules.ChangeStock(service, request.Quantity);
            var order = await db.BookingServices.FindAsync(request.BookingId, request.ServiceId);
            if (order == null)
            {
                order = new SHBMS.Model.BookingService
                {
                    BookingId = request.BookingId, ServiceId = request.ServiceId,
                    UnitPrice = request.UnitPrice, OrderedByEmployee = employeeId
                };
                db.BookingServices.Add(order);
            }
            else if (order.UnitPrice != request.UnitPrice)
                throw new InvalidOperationException("The booked service price differs from this request. Review the service charge before completing it.");
            order.Quantity = checked(order.Quantity + request.Quantity);
            request.BilledAt = now;
        }
        request.RequestStatus = status;
        request.HandledByEmployeeId = employeeId;
        request.ResolvedAt = status is "Resolved" or "Rejected" ? now : null;
        // Charge, stock and request status succeed or roll back together.
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }
}
