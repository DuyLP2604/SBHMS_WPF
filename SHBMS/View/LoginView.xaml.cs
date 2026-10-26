using SHBMS.Helper;
using SHBMS.Service.Implementation;
using SHBMS.Service.Interface;
using System.Windows;

namespace SHBMS.View;

public partial class LoginView : Window
{
    private readonly IAuthenticationService _authService;

    public LoginView()
    {
        InitializeComponent();
        _authService = new AuthenticationService();
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        string username = TxtUsername.Text.Trim();
        string password = TxtPassword.Password.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TxtError.Text = "Please enter Username and Password!";
            return;
        }

        // block spam click button
        var btn = sender as System.Windows.Controls.Button;
        if (btn != null) btn.IsEnabled = false;

        var account = await _authService.Login(username, password);

        if (account != null)
        {
            //Save to session
            Session.CurrentUser = account;

            MainWindow mainWindow = new MainWindow();
            this.Close();
            mainWindow.Show();
        }
        else
        {
            TxtError.Text = "Invalid username or password.";
        }

        if (btn != null) btn.IsEnabled = true;
    }
}