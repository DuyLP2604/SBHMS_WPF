namespace SBHMS.Services.Interfaces
{
    public interface IAuthenticationService
    {
        // [UC-01] Login
        Task<AccountDto> LoginAsync(string username, string password);

        // [UC-02] Logout
        Task LogoutAsync(int userId);

        // [UC-03] Change Password
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    }
}