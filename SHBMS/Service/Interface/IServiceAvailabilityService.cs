
using SHBMS.DTO.Service;

namespace SHBMS.Service.Interface;

public interface IServiceAvailabilityService
{
    // UC-18
    Task<List<ServiceAvailabilityDto>> GetServicesAsync();

    // UC-19
    Task<bool> UpdateAvailabilityAsync(
        string serviceId,
        bool isAvailable,
        int? stockQuantity, ServiceAvailabilityDto? original = null);
}
