using SBHMS.DTO.Revenue;
using SBHMS.Services.Interfaces;

namespace SBHMS.Services.Implementations
{
    public class RevenueAndPricingService : IRevenueAndPricingService
    {
        private readonly PRN212_SBHMSContext _context;
        public RevenueAndPricingService(PRN212_SBHMSContext context) { _context = context; }

        public Task<RevenueReportDto> GetDailyRevenueReportAsync(DateTime date) => throw new NotImplementedException();
        public Task<RevenueReportDto> GetMonthlyRevenueReportAsync(int year, int month) => throw new NotImplementedException();
        public Task<IEnumerable<PromotionDto>> GetAllPromotionsAsync() => throw new NotImplementedException();
        public Task<bool> CreatePromotionAsync(PromotionCreateDto dto) => throw new NotImplementedException();
        public Task<bool> UpdateRoomPricingRuleAsync(PricingRuleDto dto) => throw new NotImplementedException();
    }
}