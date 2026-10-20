
using System.Windows;
using System.ComponentModel;
using SHBMS.ViewModel;

namespace SHBMS.View
{
    public partial class MainWindow : Window
    {
        // Keep views alive when switching
        private readonly MainViewModel _viewModel = new();
        private readonly CustomerProfilesView _customers = new();
        private readonly GuestFeedbackView _feedback = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            _viewModel.PropertyChanged += ModuleNavigationChanged;

            // Default page
            ShowCustomerPage();
        }

        private void ModuleNavigationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MainViewModel.CurrentPageKey) || !_viewModel.IsOperationsPage)
                return;
            CustomerNavButton.Tag = null;
            FeedbackNavButton.Tag = null;
        }

        // =====================================
        // CUSTOMER PROFILES
        // =====================================
        private void Customer_Click(
            object sender,
            RoutedEventArgs e)
        {
            _viewModel.ShowLegacyPage("Customers");
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
            _viewModel.ShowLegacyPage("Feedback");
            MainContent.Content = _feedback;

            // Highlight selected menu
            CustomerNavButton.Tag = null;
            FeedbackNavButton.Tag = "Selected";
        }
    }
}
