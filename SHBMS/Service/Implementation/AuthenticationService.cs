using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Auth;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Interface;
namespace SHBMS.Service.Implementation;

public class AuthenticationService : IAuthenticationService
{
    // Asynchronous method to handle user login 
    public async Task<AccountDto?> Login(string username, string password)
    {
        // Validate that both username and password are provided    
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return null;
        // Create a new instance of the database context to interact with the database
        using var db = new Prn212SbhmsContext();
        // Query the database for a user with the provided username, including related employee data
        var user = await db.Users.AsNoTracking().Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username == username.Trim());
        // If the user is not found or the password does not match, return null
        if (user == null || !PasswordHelper.Verify(password, user.Password)) return null;
        if (!user.IsActive)
            throw new InvalidOperationException("Your account has been disabled. Please contact the administrator.");
        var account = new AccountDto
        {
            UserId = user.UserId,
            EmployeeId = user.Employee?.EmployeeId,
            Username = user.Username,
            Role = user.Role,
            FullName = user.Employee?.FullName ?? user.Username,
            Email = user.Employee?.Email,
            Phone = user.Employee?.Phone,
            IsActive = user.IsActive
        };
        Session.SetAccount(account);
        return account;
    }
    public Task Logout(int userId) { Session.Clear(); return Task.CompletedTask; }
    public async Task<bool> ChangePassword(int userId, string oldPassword, string newPassword)
    {
        if (newPassword.Length < 6) return false;
        using var db = new Prn212SbhmsContext();
        var user = await db.Users.FindAsync(userId);
        if (user == null || !PasswordHelper.Verify(oldPassword, user.Password)) return false;
        user.Password = PasswordHelper.Hash(newPassword);
        await db.SaveChangesAsync();
        return true;
    }
}
