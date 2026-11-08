using Microsoft.EntityFrameworkCore;
using SHBMS.Helper;
using SHBMS.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Mail;
using System.Windows.Data;
namespace SHBMS.ViewModel;
public class StaffAccountViewModel : DatabaseViewModel
{
    public ObservableCollection<User> Accounts { get; } = new();
    public ICollectionView AccountsView { get; }
    public string[] Roles { get; } = { "Admin", "Manager", "Staff" };
    private string _username = "";
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    private string _fullName = "";
    public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
    private string _phone = "";
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    private string _email = "";
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    private string _role = "";
    public string Role { get => _role; set => SetProperty(ref _role, value); }
    private string _newPassword = "";
    public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }
    private string _searchText = "";
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    private User? _selectedAccount;
    public bool IsNew => SelectedAccount == null;
    public User? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            SetProperty(ref _selectedAccount, value);
            Username = value?.Username ?? ""; FullName = value?.Employee?.FullName ?? "";
            Phone = value?.Employee?.Phone ?? ""; Email = value?.Employee?.Email ?? "";
            Role = value?.Role ?? "Staff"; NewPassword = ""; OnPropertyChanged(nameof(IsNew));
        }
    }
    public RelayCommand NewCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ShowAllCommand { get; }
    public StaffAccountViewModel()
    {
        AccountsView = CollectionViewSource.GetDefaultView(Accounts);
        AccountsView.Filter = item => item is User u && (string.IsNullOrWhiteSpace(SearchText)
            || Contains(u.Username, SearchText.Trim()) || Contains(u.Employee?.FullName, SearchText.Trim())
            || Contains(u.Role, SearchText.Trim()));
        NewCommand = new RelayCommand(_ => SelectedAccount = null, _ => !IsBusy && Session.IsAdmin);
        SaveCommand = DatabaseCommand(SaveAsync, () => Session.IsAdmin);
        SearchCommand = new RelayCommand(_ => AccountsView.Refresh(), _ => !IsBusy);
        ShowAllCommand = new RelayCommand(_ => { SearchText = ""; AccountsView.Refresh(); }, _ => !IsBusy);
        SelectedAccount = null;
    }
    protected override async Task LoadDataAsync()
    {
        if (!Session.IsAdmin) { StatusMessage = "Only Admin can manage staff accounts."; return; }
        using var db = new Prn212SbhmsContext();
        var data = await db.Users.AsNoTracking().Include(u => u.Employee).OrderBy(u => u.Username).ToListAsync();
        Accounts.Clear(); foreach (var u in data) Accounts.Add(u); SelectedAccount = null;
    }
    private async Task SaveAsync()
    {
        if (!Session.IsAdmin) return;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Phone)
            || !Roles.Contains(Role)) { StatusMessage = "Username, full name, phone and a valid role are required."; return; }
        if (Username.Trim().Length > 50 || FullName.Trim().Length > 100 || Phone.Trim().Length > 20 || Email.Trim().Length > 100)
        { StatusMessage = "A field exceeds its allowed length."; return; }
        if (!string.IsNullOrWhiteSpace(Email) && !MailAddress.TryCreate(Email.Trim(), out _))
        { StatusMessage = "Invalid email address."; return; }
        if ((IsNew || NewPassword.Length > 0) && NewPassword.Length < 6)
        { StatusMessage = "Password must have at least 6 characters."; return; }
        if (SelectedAccount is { } selected && selected.UserId == Session.CurrentAccount?.UserId && Role != selected.Role)
        { StatusMessage = "Use another administrator account to change your own role."; return; }
        using var db = new Prn212SbhmsContext();
        int? id = SelectedAccount?.UserId;
        if (await db.Users.AnyAsync(u => u.Username == Username.Trim() && u.UserId != id))
        { StatusMessage = "Username already exists."; return; }
        var user = id == null ? new User { Username = Username.Trim(), Password = PasswordHelper.Hash(NewPassword) }
            : await db.Users.Include(u => u.Employee).FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) { StatusMessage = "Account no longer exists. Refresh the list."; return; }
        user.Role = Role;
        if (NewPassword.Length > 0) user.Password = PasswordHelper.Hash(NewPassword);
        user.Employee ??= new Employee
        {
            EmployeeId = EntityIds.Next(await db.Employees.Select(e => e.EmployeeId).ToListAsync(), "EMP", 3)
        };
        user.Employee.FullName = FullName.Trim(); user.Employee.Phone = Phone.Trim();
        user.Employee.Email = EntityIds.Optional(Email); user.Employee.Position = Role;
        if (id == null) db.Users.Add(user);
        await db.SaveChangesAsync(); await LoadDataAsync(); StatusMessage = "Account saved to database.";
    }
}
