using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Auth;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Interface;
namespace SHBMS.Service.Implementation;
public class AuthenticationService : IAuthenticationService
{
    public async Task<AccountDto?> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return null;
        using var db = new Prn212SbhmsContext();
        // Preserve the first-run account. User and Employee are saved in one operation.
        if (!await db.Users.AnyAsync())
        {
            db.Users.Add(new User
            {
                Username = "admin", Password = PasswordHelper.Hash("123456"), Role = "Admin",
                Employee = new Employee
                {
                    EmployeeId = EntityIds.Next(await db.Employees.Select(e => e.EmployeeId).ToListAsync(), "EMP", 3),
                    FullName = "System Administrator", Position = "Administrator",
                    Phone = "0123456789", Email = "admin@shbms.local"
                }
            });
            await db.SaveChangesAsync();
        }
        var user = await db.Users.AsNoTracking().Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username == username.Trim());
        if (user == null || !PasswordHelper.Verify(password, user.Password)) return null;
        var account = new AccountDto
        {
            UserId = user.UserId, EmployeeId = user.Employee?.EmployeeId,
            Username = user.Username, Role = user.Role,
            FullName = user.Employee?.FullName ?? user.Username,
            Email = user.Employee?.Email, Phone = user.Employee?.Phone
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
