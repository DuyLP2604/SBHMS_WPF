
using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Service;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation;

public class ServiceAvailabilityService : IServiceAvailabilityService
{
    private readonly Func<Prn212SbhmsContext> _createContext;

    public ServiceAvailabilityService(
        Func<Prn212SbhmsContext>? createContext = null)
    {
        _createContext = createContext
            ?? (() => new Prn212SbhmsContext());
    }

    // UC-18: View Service Availability
    public async Task<List<ServiceAvailabilityDto>> GetServicesAsync()
    {
        if (Session.GetRole() is not ("Staff" or "Manager"))
            throw new UnauthorizedAccessException(
                "Only Staff and Manager can view services.");

        using var db = _createContext();

        return await db.Services
            .AsNoTracking()
            .OrderBy(s => s.ServiceId)
            .Select(s => new ServiceAvailabilityDto
            {
                ServiceId = s.ServiceId,
                ServiceName = s.ServiceName,
                UnitPrice = s.UnitPrice,
                IsAvailable = s.IsAvailable,
                StockQuantity = s.StockQuantity
            })
            .ToListAsync();
    }

    // UC-19: Update Service Availability
    public async Task<bool> UpdateAvailabilityAsync(
        string serviceId,
        bool isAvailable,
        int? stockQuantity)
    {
        if (!Session.IsStaff)
            throw new UnauthorizedAccessException(
                "Only Staff can update service availability.");

        if (string.IsNullOrWhiteSpace(serviceId))
            throw new ArgumentException(
                "Please select a service.");

        if (stockQuantity < 0)
            throw new ArgumentException(
                "Stock quantity cannot be negative.");

        if (isAvailable && stockQuantity == 0)
            throw new ArgumentException(
                "A service with zero stock cannot be available.");

        using var db = _createContext();

        var service = await db.Services
            .FirstOrDefaultAsync(s => s.ServiceId == serviceId);

        if (service == null)
            return false;

        service.IsAvailable = isAvailable;
        service.StockQuantity = stockQuantity;

        await db.SaveChangesAsync();

        return true;
    }
}
