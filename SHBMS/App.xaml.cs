using Microsoft.EntityFrameworkCore;
using SHBMS.Helper;
using SHBMS.Model;
using System.IO;
using System.Windows;
namespace SHBMS;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // The scaffolded context reads JSON from the current directory.
        // Configure the directory here; keep generated Model files unchanged.
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        base.OnStartup(e);
        try
        {
            InitializeAccounts();
        }
        catch (Exception ex)
        {
            // StartupUri still opens LoginView after this handler returns.
            MessageBox.Show(
                "Không khởi tạo được tài khoản. " +
                "Hãy kiểm tra SQL Server và appsettings.json.\n\n" +
                ex.GetBaseException().Message,
                "Khởi tạo dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static void InitializeAccounts()
    {
        using var db = new Prn212SbhmsContext();

        // Reserve employee IDs in memory as well as in the database, because all
        // three accounts are saved together at the end of this method.
        var employeeIds = db.Employees.Select(e => e.EmployeeId).ToList();

        EnsureAccount(db, employeeIds,
            "admin", "123456", "Admin", "Nguyễn Thành Đạt", "0901000013", "nv13.hbms@gmail.com");
        EnsureAccount(db, employeeIds,
            "manager", "123123", "Manager", "Trần Quốc Bảo", "0901000003", "nv03.hbms@gmail.com");
        EnsureAccount(db, employeeIds,
            "staff", "13579", "Staff", "Nguyễn Minh Anh", "0901000002", "nv02.hbms@gmail.com");

        // EF saves new Users and their Employees in one transaction, assigning
        // the identity UserId before inserting each dependent Employee.
        db.SaveChanges();
    }

    private static void EnsureAccount(
        Prn212SbhmsContext db,
        List<string> employeeIds,
        string username,
        string password,
        string role,
        string fullName,
        string phone,
        string email)
    {
        var user = db.Users.Include(u => u.Employee)
            .FirstOrDefault(u => u.Username == username);

        if (user == null)
        {
            user = new User { Username = username };
            db.Users.Add(user);
        }

        // Keep the three configured logins available for the project.
        // Other user accounts are left unchanged.
        user.Password = PasswordHelper.Hash(password);
        user.Role = role;

        if (user.Employee != null)
        {
            // Replace only the placeholder profiles from the earlier initializer.
            // Preserve a profile that someone has already edited.
            if (user.Employee.FullName is "Test Administrator" or "Test Manager" or "Test Staff"
                || user.Employee.FullName == "Nhân viên mẫu (" + username + ")")
            {
                user.Employee.FullName = fullName;
                user.Employee.Phone = phone;
                user.Employee.Email = email;
            }
            user.Employee.Position = role;
            return;
        }

        string employeeId = EntityIds.Next(employeeIds, "EMP", 3);
        employeeIds.Add(employeeId);

        var employee = new Employee
        {
            EmployeeId = employeeId,
            FullName = fullName,
            Position = role,
            Phone = phone,
            Email = email,
            User = user
        };

        db.Employees.Add(employee);
    }
}
