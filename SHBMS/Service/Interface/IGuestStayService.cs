namespace SBHMS.Services.Interfaces
{
    public interface IGuestStayService
    {
        // [UC-07] Check In Guest
        Task<bool> CheckInGuestAsync(CheckInDto dto);

        // [UC-08] Check Out Guest
        Task<InvoiceSummaryDto> CheckOutGuestAsync(int stayId);

        // [UC-16] Manage Customer Profiles
        Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string keyword);
        Task<CustomerDto> GetCustomerByIdAsync(int customerId);
        Task<bool> CreateCustomerProfileAsync(CustomerCreateDto dto);
        Task<bool> UpdateCustomerProfileAsync(CustomerUpdateDto dto);
    }
}