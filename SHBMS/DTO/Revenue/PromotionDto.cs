namespace SBHMS.DTO.Revenue;

public class PromotionDto
{
    public int PromotionId { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public decimal DiscountPercentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}
