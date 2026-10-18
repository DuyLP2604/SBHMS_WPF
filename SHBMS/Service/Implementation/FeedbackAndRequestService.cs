using SBHMS.DTO;
using SBHMS.Services.Interfaces;

namespace SBHMS.Services.Implementations
{
    public class FeedbackAndRequestService : IFeedbackAndRequestService
    {
        private readonly PRN212_SBHMSContext _context;
        public FeedbackAndRequestService(PRN212_SBHMSContext context) { _context = context; }

        public Task<IEnumerable<ServiceRequestDto>> GetActiveRequestsAsync() => throw new NotImplementedException();
        public Task<bool> UpdateRequestStatusAsync(int requestId, RequestStatusEnum status) => throw new NotImplementedException();
        public Task<bool> SubmitGuestFeedbackResolutionAsync(int feedbackId, string resolutionDetails, int staffId) => throw new NotImplementedException();
        public Task<IEnumerable<GuestFeedbackDto>> GetAllGuestFeedbacksAsync() => throw new NotImplementedException();
        public Task<FeedbackSummaryDto> GetFeedbackAnalyticsAsync() => throw new NotImplementedException();
    }
}