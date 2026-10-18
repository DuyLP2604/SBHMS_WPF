namespace SBHMS.DTO
{
    public class FeedbackSummaryDto
    {
        public double AverageRating { get; set; }
        public int TotalFeedbacks { get; set; }
        public int ResolvedFeedbacks { get; set; }
        public int PendingFeedbacks { get; set; }
    }
}