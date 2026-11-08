using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Service;
using SHBMS.Model;
using System.Data;

namespace SHBMS.Service.Implementation;

// Booking services places orders. The processing screen completes or rejects them.
public class BookingServicesManager
{
    private readonly Func<Prn212SbhmsContext> _createContext;
    public BookingServicesManager(Func<Prn212SbhmsContext>? createContext = null)
        => _createContext = createContext ?? (() => new Prn212SbhmsContext());

    public async Task<List<Booking>> GetBookingsAsync()
    {
        using var db = _createContext();
        await ServiceWorkflowRules.VerifyViewerAsync(db);
        return await db.Bookings.AsNoTracking().Include(b => b.Customer)
            .Where(b => !b.IsDeleted && b.BookingStatus == "CHECKED_IN" && !b.Invoices.Any())
            .OrderBy(b => b.BookingId).ToListAsync();
    }

    public async Task<List<SHBMS.Model.Service>> GetServicesAsync()
    {
        using var db = _createContext();
        await ServiceWorkflowRules.VerifyViewerAsync(db);
        return await db.Services.AsNoTracking().OrderBy(s => s.ServiceName).ToListAsync();
    }

    public async Task<List<GuestServiceRequest>> GetOrdersAsync()
    {
        using var db = _createContext();
        await ServiceWorkflowRules.VerifyViewerAsync(db);
        var orders = await ServiceWorkflowRules.QueryRequests(db).ToListAsync();
        var charges = await db.BookingServices.AsNoTracking().Include(o => o.Service)
            .Include(o => o.Booking).ThenInclude(b => b.Customer)
            .Where(o => !o.Booking.IsDeleted).OrderBy(o => o.BookingId).ThenBy(o => o.ServiceId).ToListAsync();
        // Keep older recorded charges visible without displaying newly resolved requests twice.
        foreach (var charge in charges)
        {
            int requestQuantity = orders.Where(r => r.BookingId == charge.BookingId
                && r.ServiceId == charge.ServiceId && r.BilledAt != null).Sum(r => r.Quantity);
            int olderQuantity = charge.Quantity - requestQuantity;
            if (olderQuantity <= 0) continue;
            orders.Add(new GuestServiceRequest
            {
                IsLegacy = true, BookingId = charge.BookingId, ServiceId = charge.ServiceId,
                CustomerId = charge.Booking.CustomerId, CustomerName = charge.Booking.Customer.FullName,
                ServiceName = charge.Service.ServiceName, Quantity = olderQuantity, UnitPrice = charge.UnitPrice,
                Status = "Recorded", Details = "Previously recorded service charge.", HandledBy = charge.OrderedByEmployee
            });
        }
        return orders.OrderByDescending(o => o.CreatedAt).ThenBy(o => o.BookingId).ToList();
    }

    public Task CreateAsync(string bookingId, string serviceId, int quantity, string? details)
        => new ServiceRequestManager(_createContext).AddAsync(new GuestServiceRequest
        {
            BookingId = bookingId, ServiceId = serviceId, Quantity = quantity,
            Details = OrderDetails(details, serviceId)
        });

    public async Task UpdatePendingAsync(int requestId, int quantity, string? details, int quotedQuantity)
    {
        if (quantity < 1) throw new ArgumentException("Quantity must be a positive whole number.");
        using var db = _createContext();
        await ServiceWorkflowRules.VerifyStaffAsync(db);
        string? bookingId = await db.ServiceRequests.Where(r => r.RequestId == requestId)
            .Select(r => r.BookingId).FirstOrDefaultAsync();
        if (bookingId == null) throw new InvalidOperationException("Service order no longer exists. Refresh the list.");
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ServiceWorkflowRules.ReadOpenBookingAsync(db, bookingId);
        var order = await db.ServiceRequests.SingleAsync(r => r.RequestId == requestId);
        if (order.RequestStatus != "Pending" || order.BilledAt != null)
            throw new InvalidOperationException("Only pending service orders can be edited. Refresh the list.");
        if (order.Quantity != quotedQuantity)
            throw new InvalidOperationException("Service quantity changed. Refresh before editing again.");
        var service = await db.Services.SingleAsync(s => s.ServiceId == order.ServiceId);
        if (!service.IsAvailable || service.StockQuantity < quantity)
            throw new InvalidOperationException("This service is unavailable or does not have enough stock.");
        order.Quantity = quantity;
        order.RequestDescription = OrderDetails(details, order.ServiceId);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task CancelPendingAsync(int requestId)
    {
        using var db = _createContext();
        string employeeId = await ServiceWorkflowRules.VerifyStaffAsync(db);
        string? bookingId = await db.ServiceRequests.Where(r => r.RequestId == requestId)
            .Select(r => r.BookingId).FirstOrDefaultAsync();
        if (bookingId == null) throw new InvalidOperationException("Service order no longer exists. Refresh the list.");
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ServiceWorkflowRules.ReadOpenBookingAsync(db, bookingId);
        var order = await db.ServiceRequests.SingleAsync(r => r.RequestId == requestId);
        if (order.RequestStatus != "Pending" || order.BilledAt != null)
            throw new InvalidOperationException("Only pending service orders can be cancelled here. Use Service requests for orders already in progress.");
        order.RequestStatus = "Rejected";
        order.ResolvedAt = DateTime.Now;
        order.HandledByEmployeeId = employeeId;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static string OrderDetails(string? details, string serviceId)
    {
        string result = string.IsNullOrWhiteSpace(details) ? $"Service order {serviceId}." : details.Trim();
        if (result.Length > 500) throw new ArgumentException("Notes cannot exceed 500 characters.");
        return result;
    }
}
