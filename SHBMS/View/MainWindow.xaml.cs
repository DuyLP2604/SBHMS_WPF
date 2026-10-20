
using System.Windows;

namespace SHBMS.View
{
    public partial class MainWindow : Window
    {
        // Keep views alive when switching
        private readonly CustomerProfilesView _customers = new();
        private readonly GuestFeedbackView _feedback = new();
        private readonly InvoiceManager _invoiceManager = new();

        public MainWindow()
        {
            InitializeComponent();

            // Default page
            ShowCustomerPage();
        }

        // =====================================
        // CUSTOMER PROFILES
        // =====================================
        private void Customer_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowCustomerPage();
        }

        private void ShowCustomerPage()
        {
            MainContent.Content = _customers;

            // Highlight selected menu
            CustomerNavButton.Tag = "Selected";
            FeedbackNavButton.Tag = null;
        }

        // =====================================
        // GUEST FEEDBACK
        // =====================================
        private void Feedback_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content = _feedback;

            // Highlight selected menu
            CustomerNavButton.Tag = null;
            FeedbackNavButton.Tag = "Selected";
        }


        private void Invoice_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content = _invoiceManager;
            // Highlight selected menu
            CustomerNavButton.Tag = null;
            FeedbackNavButton.Tag = null;
            InvoiceNavButton.Tag = "Selected";
        }
    }
}
