using SHBMS.Model;
using SHBMS.DTO.Guest;
using SHBMS.DTO.Invoice;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation
{
    public class GuestStayService : IGuestStayService
    {
        private readonly Prn212SbhmsContext _context;
        public GuestStayService(Prn212SbhmsContext context) { _context = context; }

        public Task<bool> CheckInGuestAsync(CheckInDto dto) => throw new NotImplementedException();
        public Task<InvoiceSummaryDto> CheckOutGuestAsync(string stayId) => throw new NotImplementedException();
        public Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string keyword) => throw new NotImplementedException();
        public Task<CustomerDto> GetCustomerByIdAsync(string customerId) => throw new NotImplementedException();
        public Task<bool> CreateCustomerProfileAsync(CustomerCreateDto dto) => throw new NotImplementedException();
        public Task<bool> UpdateCustomerProfileAsync(CustomerUpdateDto dto) => throw new NotImplementedException();
    }
}
