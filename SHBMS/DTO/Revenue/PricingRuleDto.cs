namespace SHBMS.DTO.Revenue;

public class PricingRuleDto
{
    public string RoomTypeId { get; set; } = "";
    public decimal SpecialPrice { get; set; }
    public DateTime EffectiveDate { get; set; }
}
