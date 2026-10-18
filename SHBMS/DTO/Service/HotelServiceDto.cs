namespace SBHMS.DTO
{
    public class HotelServiceDto
    {
        public int ServiceId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; } 
        public bool IsAvailable { get; set; }
        public int StockQuantity { get; set; }
    }
}