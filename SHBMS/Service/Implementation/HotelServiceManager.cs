using SBHMS.DTO;
using SBHMS.Services.Interfaces;

namespace SBHMS.Services.Implementations
{
    public class HotelServiceManager : IHotelServiceManager
    {
        private readonly PRN212_SBHMSContext _context;
        public HotelServiceManager(PRN212_SBHMSContext context) { _context = context; }

        public Task<IEnumerable<HotelServiceDto>> GetAvailableServicesAsync() => throw new NotImplementedException();
        public Task<bool> UpdateServiceStatusAsync(int serviceId, bool isAvailable, int stockQuantity) => throw new NotImplementedException();
        public Task<bool> OrderAdditionalServiceAsync(ServiceOrderCreateDto dto) => throw new NotImplementedException();
        public Task<IEnumerable<ServiceOrderDto>> GetOrdersByStayIdAsync(int stayId) => throw new NotImplementedException();
    }
}