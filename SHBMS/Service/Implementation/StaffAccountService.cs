using SBHMS.DTO.Auth;
using SBHMS.Services.Interfaces;

namespace SBHMS.Services.Implementations
{
    public class StaffAccountService : IStaffAccountService
    {
        private readonly PRN212_SBHMSContext _context;
        public StaffAccountService(PRN212_SBHMSContext context) { _context = context; }

        public Task<IEnumerable<AccountDto>> GetAllStaffAccountsAsync() => throw new NotImplementedException();
        public Task<AccountDto> GetStaffAccountByIdAsync(int accountId) => throw new NotImplementedException();
        public Task<bool> CreateStaffAccountAsync(AccountCreateDto dto) => throw new NotImplementedException();
        public Task<bool> UpdateStaffAccountAsync(AccountUpdateDto dto) => throw new NotImplementedException();
        public Task<bool> ToggleAccountStatusAsync(int accountId, bool isActive) => throw new NotImplementedException();
    }
}