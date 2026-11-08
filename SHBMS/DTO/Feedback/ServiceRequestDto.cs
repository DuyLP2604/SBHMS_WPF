namespace SHBMS.DTO
{
    public class ServiceRequestDto
    {
        public int RequestId { get; set; }
        public string RoomId { get; set; } = "";
        public string RoomNumber { get; set; } = "";
        public string RequestDetails { get; set; } = "";
        public RequestStatusEnum Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
