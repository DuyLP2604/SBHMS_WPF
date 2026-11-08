using SHBMS.DTO.Invoice;
using SHBMS.DTO.Guest;
namespace SHBMS.Service.Interface
{
    public interface IGuestStayService
    {
        // [UC-07] Check In Guest
        Task<bool> CheckInGuestAsync(CheckInDto dto);

        // [UC-08] Check Out Guest
        Task<InvoiceSummaryDto> CheckOutGuestAsync(string stayId);

        // [UC-16] Manage Customer Profiles
        Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string keyword);
        Task<CustomerDto> GetCustomerByIdAsync(string customerId);
        Task<bool> CreateCustomerProfileAsync(CustomerCreateDto dto);
        Task<bool> UpdateCustomerProfileAsync(CustomerUpdateDto dto);
    }
}
