using SHBMS.DTO.Auth;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Implementation;
using SHBMS.Service.Interface;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Mail;
using System.Text.RegularExpressions;
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
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }

    public RelayCommand NewCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ShowAllCommand { get; }
    public RelayCommand DisableCommand { get; }
    public RelayCommand EnableCommand { get; }


    public StaffAccountViewModel() : this(new StaffAccountService(new Prn212SbhmsContext())) { }

    public StaffAccountViewModel(IStaffAccountService service)
    {
        _service = service;

        AccountsView = CollectionViewSource.GetDefaultView(Accounts);
        AccountsView.Filter = item => item is AccountDto u && (string.IsNullOrWhiteSpace(SearchText)
            || Contains(u.Username, SearchText.Trim())
            || Contains(u.FullName, SearchText.Trim())
            || Contains(u.Role, SearchText.Trim()));

        // Only Admin can create new accounts, so disable the NewCommand for non-admin users
        NewCommand = new RelayCommand(_ => SelectedAccount = null, _ => !IsBusy && Session.IsAdmin);

        // Only Admin can save accounts, so disable the SaveCommand for non-admin users
        SaveCommand = DatabaseCommand(SaveAsync, () => Session.IsAdmin);
        DisableCommand = DatabaseCommand(DisableAccountAsync, () => Session.IsAdmin && SelectedAccount is { IsActive: true });
        EnableCommand = DatabaseCommand(EnableAccountAsync, () => Session.IsAdmin && SelectedAccount is { IsActive: false }); SearchCommand = new RelayCommand(_ => AccountsView.Refresh(), _ => !IsBusy);
        ShowAllCommand = new RelayCommand(_ => { SearchText = ""; AccountsView.Refresh(); }, _ => !IsBusy);
        SelectedAccount = null;
    }

    protected override async Task LoadDataAsync()
    {
        // Check if the current user is an Admin before loading staff accounts
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

        if (!Regex.IsMatch(Phone.Trim(), @"^(0|\+84)[3|5|7|8|9][0-9]{8}$"))
        {
            StatusMessage = "Invalid phone number format.";
            return;
        }

        if ((IsNew || NewPassword.Length > 0) && NewPassword.Length < 6)
        {
            StatusMessage = "Password must have at least 6 characters.";
            return;
        }

        // Do not allow an admin to change their own role to a non-admin role
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
                success = await _service.UpdatePasswordAsync(SelectedAccount!.UserId, NewPassword);
                if (!success) StatusMessage = "Account updated, but the password could not be changed.";
            }

        }

        if (success)
        {
            await LoadDataAsync();
            StatusMessage = "Account saved successfully.";
        }
    }
    private async Task DisableAccountAsync()
    {
        if (!Session.IsAdmin || SelectedAccount == null) return;

        // Prevent an admin from disabling their own account
        if (SelectedAccount.UserId == Session.CurrentAccount?.UserId)
        {
            StatusMessage = "You cannot disable your own account.";
            return;
        }

        if (!SelectedAccount.IsActive)
        {
            StatusMessage = $"Account '{SelectedAccount.Username}' is already disabled.";
            return;
        }

        // Keep the values locally because LoadDataAsync() resets SelectedAccount to null
        int userId = SelectedAccount.UserId;
        string username = SelectedAccount.Username;

        var result = MessageBox.Show(
            $"Are you sure you want to disable the account '{username}'?\n" +
            "The employee will no longer be able to log in, but their booking and payment history will be kept.",
            "Confirm Disable",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        bool success = await _service.DeactivateStaffAccountAsync(userId);
        if (success) await LoadDataAsync();

        StatusMessage = success
            ? $"Account '{username}' disabled successfully."
            : "Failed to disable account. It may no longer exist.";
    }

    private async Task EnableAccountAsync()
    {
        if (!Session.IsAdmin || SelectedAccount == null || SelectedAccount.IsActive) return;

        int userId = SelectedAccount.UserId;
        string username = SelectedAccount.Username;

        bool success = await _service.ReactivateStaffAccountAsync(userId);
        if (success) await LoadDataAsync();

        StatusMessage = success
            ? $"Account '{username}' enabled successfully."
            : "Failed to enable account. It may no longer exist.";
    }
}