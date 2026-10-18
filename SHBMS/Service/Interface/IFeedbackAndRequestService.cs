namespace SBHMS.Services.Interfaces
{
    public interface IFeedbackAndRequestService
    {
        // [UC-15] Process Service Request
        Task<IEnumerable<ServiceRequestDto>> GetActiveRequestsAsync();
        Task<bool> UpdateRequestStatusAsync(int requestId, RequestStatusEnum status);

        // [UC-23] Resolve Guest Feedback (Staff)
        Task<bool> SubmitGuestFeedbackResolutionAsync(int feedbackId, string resolutionDetails, int staffId);

        // [UC-24] Review Guest Feedback (Manager)
        Task<IEnumerable<GuestFeedbackDto>> GetAllGuestFeedbacksAsync();
        Task<FeedbackSummaryDto> GetFeedbackAnalyticsAsync();
    }
}