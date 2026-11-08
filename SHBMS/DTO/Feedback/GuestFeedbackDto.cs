namespace SHBMS.DTO
{
    // DTO Đánh giá phản hồi khách hàng (UC-23, UC-24)
    public class GuestFeedbackDto
    {
        public int FeedbackId { get; set; }
        public string CustomerId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public int Rating { get; set; } // 1 - 5 sao
        public string Comment { get; set; } = "";
        public string ResolutionDetails { get; set; } = "";
        public bool IsResolved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
