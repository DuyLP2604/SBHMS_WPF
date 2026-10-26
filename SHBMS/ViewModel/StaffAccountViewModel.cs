using SHBMS.Helper;
using SHBMS.Model;
using System.Collections.ObjectModel;

namespace SHBMS.ViewModel
{
    public class StaffAccountViewModel : BaseViewModel
    {
        private readonly List<StaffAccountModel> _allAccounts = new();
        private ObservableCollection<StaffAccountModel> _accounts = new();
        private StaffAccountModel? _selectedAccount;
        private string _username = string.Empty;
        private string _fullName = string.Empty;
        private string _selectedRole = "Staff";
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private string _password = string.Empty;
        private bool _isUsernameReadOnly;
        private string _searchKeyword = string.Empty;

        public List<string> Roles { get; } = new() { "Admin", "Manager", "Staff" };

        public ObservableCollection<StaffAccountModel> Accounts
        {
            get => _accounts;
            set => SetProperty(ref _accounts, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsUsernameReadOnly
        {
            get => _isUsernameReadOnly;
            set => SetProperty(ref _isUsernameReadOnly, value);
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }
        //Form action
        public StaffAccountModel? SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (SetProperty(ref _selectedAccount, value))
                {
                    OnSelectedAccountChanged();
                }
            }
        }

        public StaffAccountViewModel()
        {
            Load();
            PrepareNewForm();
        }
        //TODO: Get data from database and load to _allAccounts and Accounts
        private void Load()
        {
            _allAccounts.Clear();
            _allAccounts.Add(new StaffAccountModel { Username = "admin01", FullName = "Nguyễn Văn Anh", Role = "Admin", Phone = "0901234567", Email = "admin@hotel.com", IsActive = true });
            _allAccounts.Add(new StaffAccountModel { Username = "mgr_binh", FullName = "Trần Thị Bình", Role = "Manager", Phone = "0912345678", Email = "binh@hotel.com", IsActive = true });
            _allAccounts.Add(new StaffAccountModel { Username = "staff_cuong", FullName = "Lê Quốc Cường", Role = "Staff", Phone = "0923456789", Email = "cuong@hotel.com", IsActive = false });

            Accounts = new ObservableCollection<StaffAccountModel>(_allAccounts);
        }

        public void PrepareNewForm()
        {
            SelectedAccount = null;
            IsUsernameReadOnly = false;
            Username = string.Empty;
            FullName = string.Empty;
            SelectedRole = "Staff";
            Phone = string.Empty;
            Email = string.Empty;
            Password = string.Empty;

            ToastService.Show("Created new staff account.", "success");
        }

        public void SaveAccount()
        {
            string username = Username.Trim();
            string fullName = FullName.Trim();
            string phone = Phone.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
            {
                ToastService.Show("Please fill in Username, Full Name and Phone.", "warning");
                return;
            }

            if (SelectedAccount == null)
            {
                if (_allAccounts.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    ToastService.Show("Username already exists!", "error");
                    return;
                }

                var newAcc = new StaffAccountModel
                {
                    Username = username,
                    FullName = fullName,
                    Role = SelectedRole,
                    Phone = phone,
                    Email = Email.Trim(),
                    Password = Password.Trim(),
                    IsActive = true
                };

                _allAccounts.Add(newAcc);
                Accounts.Add(newAcc);
                ToastService.Show("Created new staff account successfully!", "success");
            }
            else
            {
                SelectedAccount.FullName = fullName;
                SelectedAccount.Role = SelectedRole;
                SelectedAccount.Phone = phone;
                SelectedAccount.Email = Email.Trim();
                if (!string.IsNullOrWhiteSpace(Password))
                    SelectedAccount.Password = Password.Trim();

                Accounts = new ObservableCollection<StaffAccountModel>(_allAccounts);
                ToastService.Show("Updated staff account successfully!", "success");
            }

            PrepareNewForm();
        }

        public void SearchAccounts()
        {
            string kw = SearchKeyword.Trim();
            if (string.IsNullOrWhiteSpace(kw))
            {
                Accounts = new ObservableCollection<StaffAccountModel>(_allAccounts);
                return;
            }

            var filtered = _allAccounts.Where(a =>
                a.Username.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                a.FullName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                a.Role.Contains(kw, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            Accounts = new ObservableCollection<StaffAccountModel>(filtered);
        }

        public void ShowAllAccounts()
        {
            SearchKeyword = string.Empty;
            Accounts = new ObservableCollection<StaffAccountModel>(_allAccounts);
        }

        public void ToggleStatus()
        {
            if (SelectedAccount != null)
            {
                SelectedAccount.IsActive = !SelectedAccount.IsActive;
                Accounts = new ObservableCollection<StaffAccountModel>(_allAccounts);
                ToastService.Show($"Successfully {(SelectedAccount.IsActive ? "unlocked" : "locked")} account {SelectedAccount.Username}.", "success");
            }
            else
            {
                ToastService.Show("Please select an account to lock/unlock.", "warning");
            }
        }

        private void OnSelectedAccountChanged()
        {
            if (SelectedAccount != null)
            {
                Username = SelectedAccount.Username;
                IsUsernameReadOnly = true;
                FullName = SelectedAccount.FullName;
                SelectedRole = SelectedAccount.Role;
                Phone = SelectedAccount.Phone;
                Email = SelectedAccount.Email;
                Password = string.Empty;
            }
        }
    }
}