using SHBMS.DTO.Auth;
namespace SHBMS.Service.Interface;

public interface IAuthenticationService
{
    // [UC-01] Login
    Task<AccountDTO> Login(string username, string password);

    // [UC-02] Logout
    Task Logout(int userId);

    // [UC-03] Change Password
    Task<bool> ChangePassword(int userId, string oldPassword, string newPassword);
}