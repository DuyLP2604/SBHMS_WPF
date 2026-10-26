using SHBMS.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SHBMS.View
{
    public partial class StaffAccountView : UserControl
    {
        private readonly StaffAccountViewModel _viewModel;

        public StaffAccountView()
        {
            InitializeComponent();
            _viewModel = new StaffAccountViewModel();
            this.DataContext = _viewModel;
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.PrepareNewForm();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SaveAccount();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SearchAccounts();
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowAllAccounts();
        }

        private void ToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ToggleStatus();
        }
    }
}