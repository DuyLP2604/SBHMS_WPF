using SHBMS.ViewModel;
using System.Windows;
using System.Windows.Controls;
namespace SHBMS.View;

public partial class StaffAccountView : UserControl
{
    StaffAccountViewModel vm;
    public StaffAccountView()
    {
        // Initialize the StaffAccountViewModel and set it as the DataContext for data binding
        InitializeComponent();
        vm = new StaffAccountViewModel();
        DataContext = vm;
        // Subscribe to the PropertyChanged event of the ViewModel to handle changes in properties
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.NewPassword) && vm.NewPassword.Length == 0)
                PasswordInput.Clear();
        };
        // Subscribe to the Loaded event of the UserControl to reload data when the view is loaded
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
    // Handle the PasswordChanged event of the PasswordBox to update the NewPassword property in the ViewModel
    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is StaffAccountViewModel vm)
            vm.NewPassword = PasswordInput.Password;
    }
}