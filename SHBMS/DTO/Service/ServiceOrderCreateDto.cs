namespace SHBMS.DTO
{
    public class ServiceOrderCreateDto
    {
        public string StayId { get; set; } = "";
        public string ServiceId { get; set; } = "";
        public int Quantity { get; set; }
        public string Note { get; set; } = "";
    }
}
