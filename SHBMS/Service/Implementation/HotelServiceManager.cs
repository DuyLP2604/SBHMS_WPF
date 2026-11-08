using SHBMS.Model;
using SHBMS.DTO;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation
{
    public class HotelServiceManager : IHotelServiceManager
    {
        private readonly Prn212SbhmsContext _context;
        public HotelServiceManager(Prn212SbhmsContext context) { _context = context; }

        public Task<IEnumerable<HotelServiceDto>> GetAvailableServicesAsync() => throw new NotImplementedException();
        public Task<bool> UpdateServiceStatusAsync(string serviceId, bool isAvailable, int stockQuantity) => throw new NotImplementedException();
        public Task<bool> OrderAdditionalServiceAsync(ServiceOrderCreateDto dto) => throw new NotImplementedException();
        public Task<IEnumerable<ServiceOrderDto>> GetOrdersByStayIdAsync(string stayId) => throw new NotImplementedException();
    }
}
