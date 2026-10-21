using System.Windows;

namespace SHBMS.View
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TxtError.Text = "Please enter Username and Password!";
                return;
            }


            if (fakeLogin(username, password))
            {
                //close and open main window
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                TxtError.Text = "Invalid username or password.";
            }
        }

        // Fake login
        //TODO: Replace with real authentication logic, Split role of manager and staff
        private bool fakeLogin(string username, string password)
        {
            // Fake login logic for demonstration purposes
            return (username == "manager" || username == "staff") && password == "123";
        }
    }
}