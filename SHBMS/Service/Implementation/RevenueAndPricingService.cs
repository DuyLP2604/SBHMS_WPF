using SHBMS.Model;
using SHBMS.DTO.Revenue;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation
{
    public class RevenueAndPricingService : IRevenueAndPricingService
    {
        private readonly Prn212SbhmsContext _context;
        public RevenueAndPricingService(Prn212SbhmsContext context) { _context = context; }

        public Task<RevenueReportDto> GetDailyRevenueReportAsync(DateTime date) => throw new NotImplementedException();
        public Task<RevenueReportDto> GetMonthlyRevenueReportAsync(int year, int month) => throw new NotImplementedException();
        public Task<IEnumerable<PromotionDto>> GetAllPromotionsAsync() => throw new NotImplementedException();
        public Task<bool> CreatePromotionAsync(PromotionCreateDto dto) => throw new NotImplementedException();
        public Task<bool> UpdateRoomPricingRuleAsync(PricingRuleDto dto) => throw new NotImplementedException();
    }
}
