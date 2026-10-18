namespace SBHMS.DTO.Revenue;

public class PricingRuleDto
{
    public int RoomTypeId { get; set; }
    public decimal SpecialPrice { get; set; }
    public DateTime EffectiveDate { get; set; }
}