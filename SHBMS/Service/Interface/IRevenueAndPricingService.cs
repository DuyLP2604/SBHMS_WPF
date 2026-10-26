namespace SHBMS.Service.Interface
{
    public interface IRevenueAndPricingService
    {
        Task<RevenueReportDto> GetDailyRevenueReportAsync(DateTime date);
        Task<RevenueReportDto> GetMonthlyRevenueReportAsync(int year, int month);

        // [UC-17] Manage Promotions & Pricing (Manager)
        Task<IEnumerable<PromotionDto>> GetAllPromotionsAsync();
        Task<bool> CreatePromotionAsync(PromotionCreateDto dto);
        Task<bool> UpdateRoomPricingRuleAsync(PricingRuleDto dto);
    }
}