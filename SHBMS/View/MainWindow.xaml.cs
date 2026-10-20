using SHBMS.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SHBMS.View
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel = new();
        private readonly CustomerProfilesView _customers = new();
        private readonly GuestFeedbackView _feedback = new();
        private readonly InvoiceManager _invoiceManager = new();
        private readonly GuestStayView _stays = new();
        private readonly RoomStatusView _rooms = new();
        private readonly StaffAccountView _staffAccounts = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            _viewModel.PropertyChanged += ModuleNavigationChanged;
            ShowCustomerPage();
        }

        private void ModuleNavigationChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MainViewModel.CurrentPageKey)
                || !_viewModel.IsOperationsPage)
                return;

            ResetNavigation();
            BreadcrumbText.SetCurrentValue(
                TextBlock.TextProperty,
                _viewModel.PageTitle);
        }

        private void Navigate(
            object page,
            Button navButton,
            string pageKey,
            string breadcrumb)
        {
            _viewModel.ShowLegacyPage(pageKey);
            MainContent.Content = page;
            BreadcrumbText.SetCurrentValue(
                TextBlock.TextProperty,
                breadcrumb);

            ResetNavigation();
            navButton.Tag = "Selected";
        }

        private void ResetNavigation()
        {
            CustomerNavButton.Tag = null;
            FeedbackNavButton.Tag = null;
            InvoiceNavButton.Tag = null;
            StayNavButton.Tag = null;
            RoomNavButton.Tag = null;
            StaffNavButton.Tag = null;
        }

        private void Customer_Click(object sender, RoutedEventArgs e)
        {
            ShowCustomerPage();
        }

        private void ShowCustomerPage()
        {
            Navigate(
                _customers,
                CustomerNavButton,
                "Customers",
                "Hotel operations / Guest relations");
        }

        private void Feedback_Click(object sender, RoutedEventArgs e)
        {
            Navigate(
                _feedback,
                FeedbackNavButton,
                "Feedback",
                "Hotel operations / Guest relations");
        }

        private void Invoice_Click(object sender, RoutedEventArgs e)
        {
            Navigate(
                _invoiceManager,
                InvoiceNavButton,
                "Invoices",
                "Hotel operations / Invoice management");
        }

        private void Stay_Click(object sender, RoutedEventArgs e)
        {
            Navigate(
                _stays,
                StayNavButton,
                "GuestStays",
                "Hotel operations / Front desk");
        }

        private void Room_Click(object sender, RoutedEventArgs e)
        {
            Navigate(
                _rooms,
                RoomNavButton,
                "RoomStatus",
                "Hotel operations / Rooms");
        }

        private void Staff_Click(object sender, RoutedEventArgs e)
        {
            Navigate(_staffAccounts, StaffNavButton, "StaffAccounts", "Hotel operations / System administration");
        }
    }
}