using SHBMS.Model;
using SHBMS.DTO;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation
{
    public class FeedbackAndRequestService : IFeedbackAndRequestService
    {
        private readonly Prn212SbhmsContext _context;
        public FeedbackAndRequestService(Prn212SbhmsContext context) { _context = context; }

        public Task<IEnumerable<ServiceRequestDto>> GetActiveRequestsAsync() => throw new NotImplementedException();
        public Task<bool> UpdateRequestStatusAsync(int requestId, RequestStatusEnum status) => throw new NotImplementedException();
        public Task<bool> SubmitGuestFeedbackResolutionAsync(int feedbackId, string resolutionDetails, string staffId) => throw new NotImplementedException();
        public Task<IEnumerable<GuestFeedbackDto>> GetAllGuestFeedbacksAsync() => throw new NotImplementedException();
        public Task<FeedbackSummaryDto> GetFeedbackAnalyticsAsync() => throw new NotImplementedException();
    }
}
