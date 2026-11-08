
using SHBMS.DTO.Service;
using SHBMS.Model;

namespace SHBMS.Service.Interface;

public interface IServiceRequestManager
{
    Task<List<GuestServiceRequest>> GetRequestsAsync();

    Task<List<Booking>> GetCheckedInBookingsAsync();

    Task<List<SHBMS.Model.Service>> GetServicesAsync();

    Task AddAsync(GuestServiceRequest request);

    Task<bool> UpdateStatusAsync(
        int id,
        string status,
        string staffId
    );
}
