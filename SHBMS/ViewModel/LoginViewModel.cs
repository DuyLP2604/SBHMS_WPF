using SHBMS.Helper;
using SHBMS.Service.Interface;

namespace SHBMS.ViewModel
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        // Private fields to hold the username, password, error message, and busy state
        private string _username = "";
        private string _password = "";
        private string _errorMessage = "";
        private bool _isBusy;

        // Property to hold the username entered by the user
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }
        // Property to hold the password entered by the user
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }
        // Property to hold error messages related to login attempts
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }
        // Property to indicate whether a login operation is in progress
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); System.Windows.Input.CommandManager.InvalidateRequerySuggested(); }
        }
        //Handle the login command and its execution logic
        public RelayCommand LoginCommand { get; }
        // Event to notify when login is successful
        public event Action? LoginSucceeded;
        // Constructor to initialize the LoginViewModel with an authentication service
        public LoginViewModel(IAuthenticationService authService)
        {
            // Initialize the authentication service and the login command
            _authService = authService;
            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsBusy);
        }
        // Asynchronous method to handle the login process
        private async Task LoginAsync()
        {   // Clear any previous error messages
            ErrorMessage = "";
            // Validate that both username and password are provided
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                // Set an error message if either field is empty and return early
                ErrorMessage = "Please enter Username and Password!";
                return;
            }
            // Attempt to log in using the authentication service
            try
            {   // Set the IsBusy flag to true to indicate that a login operation is in progress
                IsBusy = true;
                // Call the authentication service to attempt login with the provided username and password
                var account = await _authService.Login(Username.Trim(), Password);

                if (account == null)
                {
                    // Set an error message if the login attempt fails and return early
                    ErrorMessage = "Invalid username or password.";
                    return;
                }
                // Invoke the LoginSucceeded event to notify subscribers that the login was successful
                LoginSucceeded?.Invoke();
            }
            catch (InvalidOperationException ex)
            {
                // Show the "account disabled" message from the service directly
                ErrorMessage = ex.Message;
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
