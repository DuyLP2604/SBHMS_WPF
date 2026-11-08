using SHBMS.DTO.Auth;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Implementation;
using SHBMS.Service.Interface;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Mail;
using System.Windows;
using System.Windows.Data;

namespace SHBMS.ViewModel;

public class StaffAccountViewModel : DatabaseViewModel
{
    private readonly IStaffAccountService _service;

    public ObservableCollection<AccountDto> Accounts { get; } = new();
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

    private AccountDto? _selectedAccount;
    public bool IsNew => SelectedAccount == null;

    public AccountDto? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            SetProperty(ref _selectedAccount, value);
            Username = value?.Username ?? "";
            FullName = value?.FullName ?? "";
            Phone = value?.Phone ?? "";
            Email = value?.Email ?? "";
            Role = value?.Role ?? "Staff";
            NewPassword = "";
            OnPropertyChanged(nameof(IsNew));
        }
    }

    public RelayCommand NewCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ShowAllCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public StaffAccountViewModel() : this(new StaffAccountService(new Prn212SbhmsContext())) { }

    public StaffAccountViewModel(IStaffAccountService service)
    {
        _service = service;

        AccountsView = CollectionViewSource.GetDefaultView(Accounts);
        AccountsView.Filter = item => item is AccountDto u && (string.IsNullOrWhiteSpace(SearchText)
            || Contains(u.Username, SearchText.Trim())
            || Contains(u.FullName, SearchText.Trim())
            || Contains(u.Role, SearchText.Trim()));

        // Chặn quyền New (Chỉ Admin mới thao tác được - độc quyền)
        NewCommand = new RelayCommand(_ => SelectedAccount = null, _ => !IsBusy && Session.IsAdmin);

        // Chặn quyền Save (Chỉ Admin mới thao tác được)
        SaveCommand = DatabaseCommand(SaveAsync, () => Session.IsAdmin);
        DeleteCommand = DatabaseCommand(DeleteAccountAsync, () => Session.IsAdmin && SelectedAccount != null);
        SearchCommand = new RelayCommand(_ => AccountsView.Refresh(), _ => !IsBusy);
        ShowAllCommand = new RelayCommand(_ => { SearchText = ""; AccountsView.Refresh(); }, _ => !IsBusy);
        SelectedAccount = null;
    }

    protected override async Task LoadDataAsync()
    {
        // Kiểm tra độc quyền Admin ngay từ khâu load
        if (!Session.IsAdmin)
        {
            StatusMessage = "Access Denied: Only Admin can view and manage staff accounts.";
            return;
        }

        var data = await _service.GetAllStaffAccountsAsync();
        Accounts.Clear();
        foreach (var u in data) Accounts.Add(u);
        SelectedAccount = null;
    }

    private async Task SaveAsync()
    {
        if (!Session.IsAdmin) return;

        // 1. Validation Logic
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Phone) || !Roles.Contains(Role))
        {
            StatusMessage = "Username, full name, phone and a valid role are required.";
            return;
        }

        if (Username.Trim().Length > 50 || FullName.Trim().Length > 100 || Phone.Trim().Length > 20 || Email.Trim().Length > 100)
        {
            StatusMessage = "A field exceeds its allowed length.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(Email) && !MailAddress.TryCreate(Email.Trim(), out _))
        {
            StatusMessage = "Invalid email address.";
            return;
        }

        if ((IsNew || NewPassword.Length > 0) && NewPassword.Length < 6)
        {
            StatusMessage = "Password must have at least 6 characters.";
            return;
        }

        // Không cho phép Admin hạ quyền chính mình
        if (SelectedAccount is { } selected && selected.UserId == Session.CurrentAccount?.UserId && Role != selected.Role)
        {
            StatusMessage = "Use another administrator account to change your own role.";
            return;
        }

        // 2. Delegate CRUD logic sang Service
        bool success;
        if (IsNew)
        {
            var createDto = new AccountCreateDto
            {
                Username = Username,
                Password = NewPassword,
                FullName = FullName,
                Email = Email,
                Phone = Phone,
                Role = Role
            };
            success = await _service.CreateStaffAccountAsync(createDto);

            if (!success) StatusMessage = "Username already exists.";
        }
        else
        {
            var updateDto = new AccountUpdateDto
            {
                AccountId = SelectedAccount!.UserId,
                FullName = FullName,
                Email = Email,
                Phone = Phone,
                Role = Role
            };
            success = await _service.UpdateStaffAccountAsync(updateDto);

            // Xử lý đổi password nếu admin gõ vào ô password
            if (success && NewPassword.Length >= 6)
            {
                // Gọi thêm Service đổi mật khẩu nếu cần (Đã triển khai UpdatePasswordAsync phía trên)
                // await _service.UpdatePasswordAsync(SelectedAccount.UserId, NewPassword);
            }
        }

        if (success)
        {
            await LoadDataAsync();
            StatusMessage = "Account saved successfully.";
        }
    }
    private async Task DeleteAccountAsync()
    {
        if (!Session.IsAdmin || SelectedAccount == null) return;

        // Không cho phép Admin tự xóa chính mình
        if (SelectedAccount.UserId == Session.CurrentAccount?.UserId)
        {
            StatusMessage = "You cannot delete your own account.";
            return;
        }

        // Hiển thị hộp thoại xác nhận
        var result = MessageBox.Show(
            $"Are you sure you want to delete the account '{SelectedAccount.Username}' and all related employee data?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        // Chỉ thực thi logic xóa, không cần IsBusy = true hay try/catch nữa
        bool success = await _service.DeleteStaffAccountAsync(SelectedAccount.UserId);

        if (success)
        {
            StatusMessage = $"Account '{SelectedAccount.Username}' deleted successfully.";
            await LoadDataAsync(); // Refresh lại DataGrid
        }
        else
        {
            StatusMessage = "Failed to delete account. It may have already been removed.";
        }
    }
}