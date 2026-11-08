using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Auth;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation
{
    public class StaffAccountService : IStaffAccountService
    {
        private readonly Prn212SbhmsContext _context;
        public StaffAccountService(Prn212SbhmsContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AccountDto>> GetAllStaffAccountsAsync()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                .OrderBy(u => u.Username)
                .ToListAsync();

            return users.Select(u => new AccountDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Role = u.Role,
                FullName = u.Employee?.FullName ?? u.Username,
                Email = u.Employee?.Email,
                Phone = u.Employee?.Phone
            });
        }

        public async Task<AccountDto> GetStaffAccountByIdAsync(int accountId)
        {
            var u = await _context.Users
                .AsNoTracking()
                .Include(user => user.Employee)
                .FirstOrDefaultAsync(user => user.UserId == accountId);

            if (u == null) return null!;

            return new AccountDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Role = u.Role,
                FullName = u.Employee?.FullName ?? u.Username,
                Email = u.Employee?.Email,
                Phone = u.Employee?.Phone
            };
        }

        public async Task<bool> CreateStaffAccountAsync(AccountCreateDto dto)
        {
            // Kiểm tra trùng Username
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username.Trim()))
                return false;

            var user = new User
            {
                Username = dto.Username.Trim(),
                Password = PasswordHelper.Hash(dto.Password),
                Role = dto.Role,
                Employee = new Employee
                {
                    // Tạo EmployeeId tự động
                    EmployeeId = await IDGenerator.NextAsync(_context.Employees.Select(e => e.EmployeeId), "EMP", 3),
                    FullName = dto.FullName.Trim(),
                    Phone = dto.Phone.Trim(),
                    Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                    Position = dto.Role
                }
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStaffAccountAsync(AccountUpdateDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.UserId == dto.AccountId);

            if (user == null) return false;

            user.Role = dto.Role;

            if (user.Employee == null)
            {
                user.Employee = new Employee
                {
                    EmployeeId = await IDGenerator.NextAsync(
                    _context.Employees.Select(e => e.EmployeeId), "EMP", 3),
                    UserId = user.UserId
                };
            }

            user.Employee.FullName = dto.FullName.Trim();
            user.Employee.Phone = dto.Phone.Trim();
            user.Employee.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            user.Employee.Position = dto.Role;

            await _context.SaveChangesAsync();
            return true;
        }

        // Dành riêng cho Update Mật khẩu 
        public async Task<bool> UpdatePasswordAsync(int accountId, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == accountId);
            if (user == null) return false;

            user.Password = PasswordHelper.Hash(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<bool> ToggleAccountStatusAsync(int accountId, bool isActive)
        {
            // Theo như file UI của bạn ghi: "Account locking is not available in the current schema"
            throw new NotSupportedException("Database schema current does not support locking accounts.");
        }
        public async Task<bool> DeleteStaffAccountAsync(int accountId)
        {
            var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.UserId == accountId);
            if (user == null) return false;
            // Xóa Employee trước, sau đó xóa User
            if (user.Employee != null)
            {
                _context.Employees.Remove(user.Employee);
            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}