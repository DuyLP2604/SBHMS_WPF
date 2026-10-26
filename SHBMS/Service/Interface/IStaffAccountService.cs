namespace SHBMS.Service.Interface
{
    public interface IStaffAccountService
    {
        // [UC-04] Manage Staff Accounts (Admin)
        Task<IEnumerable<AccountDto>> GetAllStaffAccountsAsync();
        Task<AccountDto> GetStaffAccountByIdAsync(int accountId);
        Task<bool> CreateStaffAccountAsync(AccountCreateDto dto);
        Task<bool> UpdateStaffAccountAsync(AccountUpdateDto dto);
        Task<bool> ToggleAccountStatusAsync(int accountId, bool isActive);
    }
}