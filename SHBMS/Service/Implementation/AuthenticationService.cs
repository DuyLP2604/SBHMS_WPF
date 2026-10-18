using SBHMS.DTO.Auth;
using SBHMS.Services.Interfaces;

namespace SBHMS.Services.Implementations
{

    public class AuthenticationService : IAuthenticationService
    {
        private readonly PRN212_SBHMSContext _context;
        public AuthenticationService(PRN212_SBHMSContext context) { _context = context; }

        public Task<AccountDto> LoginAsync(string username, string password) => throw new NotImplementedException();
        public Task LogoutAsync(int userId) => throw new NotImplementedException();
        public Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword) => throw new NotImplementedException();
    }
}