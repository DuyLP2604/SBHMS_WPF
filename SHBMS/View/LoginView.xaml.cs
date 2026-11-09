using SHBMS.Service.Implementation;
using SHBMS.ViewModel;
using System.Windows;
namespace SHBMS.View;

public partial class LoginView : Window
{
    private readonly LoginViewModel _vm;
    public LoginView()
    {
        // Initialize the LoginViewModel with an instance of AuthenticationService
        _vm = new LoginViewModel(new AuthenticationService());
        // Set the DataContext of the view to the LoginViewModel
        InitializeComponent();
        // Set the DataContext of the view to the LoginViewModel
        DataContext = _vm;
        // Subscribe to the LoginSucceeded event to handle successful login
        _vm.LoginSucceeded += () =>
        {
            var main = new MainWindow();
            Application.Current.MainWindow = main;
            main.Show();
            Close();
        };
    }
    // PasswordBox.Password does not support an ordinary WPF Binding.
    private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        => _vm.Password = ((System.Windows.Controls.PasswordBox)sender).Password;
}
