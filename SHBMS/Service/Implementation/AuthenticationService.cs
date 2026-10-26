using SHBMS.DTO.Auth;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation
{

    public class AuthenticationService : IAuthenticationService
    {
        public async Task<AccountDTO?> Login(string username, string password)
        {
            await Task.Delay(300);

            // TODO: DB
            // Get username from database and check password
            // Get user ID for check role
            // Get role from database
            // Switch case for role to return AccountDTO
            if ((username == "manager" || username == "staff" || username == "admin") && password == "123")
            {
                return new AccountDTO
                {
                    UserId = 1,
                    Username = username,
                    FullName = "Tài khoản " + username,
                    Role = username == "manager" ? "Manager" : (username == "admin" ? "Admin" : "Staff")
                };
            }
            return null;
        }

        public Task Logout(int userId) => throw new NotImplementedException();
        public Task<bool> ChangePassword(int userId, string oldPassword, string newPassword) => throw new NotImplementedException();
    }
}