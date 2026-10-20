using System.Windows;
using System.Windows.Controls;

namespace SHBMS.View
{
    public partial class MainWindow : Window
    {
        // Keep views alive when switching
        private readonly CustomerProfilesView _customers = new();
        private readonly GuestFeedbackView _feedback = new();
        private readonly GuestStayView _stays = new();
        private readonly RoomStatusView _rooms = new();

        public MainWindow()
        {
            InitializeComponent();

            // Default page
            ShowCustomerPage();
        }

        // Show one page and highlight its menu button
        private void Navigate(UserControl page, Button navButton, string breadcrumb)
        {
            MainContent.Content = page;
            BreadcrumbText.Text = breadcrumb;

            CustomerNavButton.Tag = null;
            FeedbackNavButton.Tag = null;
            StayNavButton.Tag = null;
            RoomNavButton.Tag = null;

            navButton.Tag = "Selected";
        }

        // =====================================
        // CUSTOMER PROFILES
        // =====================================
        private void Customer_Click(object sender, RoutedEventArgs e)
        {
            ShowCustomerPage();
        }

        private void ShowCustomerPage()
        {
            Navigate(_customers, CustomerNavButton, "Hotel operations / Guest relations");
        }

        // =====================================
        // GUEST FEEDBACK
        // =====================================
        private void Feedback_Click(object sender, RoutedEventArgs e)
        {
            Navigate(_feedback, FeedbackNavButton, "Hotel operations / Guest relations");
        }

        // =====================================
        // GUEST STAYS (check-in / check-out)
        // =====================================
        private void Stay_Click(object sender, RoutedEventArgs e)
        {
            Navigate(_stays, StayNavButton, "Hotel operations / Front desk");
        }

        // =====================================
        // ROOM STATUS
        // =====================================
        private void Room_Click(object sender, RoutedEventArgs e)
        {
            Navigate(_rooms, RoomNavButton, "Hotel operations / Rooms");
        }
    }
}
