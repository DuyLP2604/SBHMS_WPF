using SHBMS.Helper;
using SHBMS.Service.Interface;

namespace SHBMS.ViewModel
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;

        private string _username = "";
        private string _password = "";
        private string _errorMessage = "";
        private bool _isBusy;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); System.Windows.Input.CommandManager.InvalidateRequerySuggested(); }
        }

        public RelayCommand LoginCommand { get; }

        public event Action? LoginSucceeded;

        public LoginViewModel(IAuthenticationService authService)
        {
            _authService = authService;
            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsBusy);
        }

        private async Task LoginAsync()
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter Username and Password!";
                return;
            }

            try
            {
                IsBusy = true;
                var account = await _authService.Login(Username.Trim(), Password);

                if (account == null)
                {
                    ErrorMessage = "Invalid username or password.";
                    return;
                }

                LoginSucceeded?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = DatabaseErrorMessages.Describe(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
