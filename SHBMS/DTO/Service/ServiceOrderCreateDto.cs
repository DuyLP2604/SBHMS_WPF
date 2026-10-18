namespace SBHMS.DTO
{
    public class ServiceOrderCreateDto
    {
        public int StayId { get; set; }
        public int ServiceId { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
    }
}