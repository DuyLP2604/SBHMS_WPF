using SBHMS.DTO.Guest;
using SBHMS.DTO.Invoice;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation
{
    public class GuestStayService : IGuestStayService
    {
        private readonly PRN212_SBHMSContext _context;
        public GuestStayService(PRN212_SBHMSContext context) { _context = context; }

        public Task<bool> CheckInGuestAsync(CheckInDto dto) => throw new NotImplementedException();
        public Task<InvoiceSummaryDto> CheckOutGuestAsync(int stayId) => throw new NotImplementedException();
        public Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string keyword) => throw new NotImplementedException();
        public Task<CustomerDto> GetCustomerByIdAsync(int customerId) => throw new NotImplementedException();
        public Task<bool> CreateCustomerProfileAsync(CustomerCreateDto dto) => throw new NotImplementedException();
        public Task<bool> UpdateCustomerProfileAsync(CustomerUpdateDto dto) => throw new NotImplementedException();
    }
}