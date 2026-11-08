using SHBMS.ViewModel;
using System.Windows;
using System.Windows.Controls;
namespace SHBMS.View;

public partial class StaffAccountView : UserControl
{
    StaffAccountViewModel vm;
    public StaffAccountView()
    {
        InitializeComponent();
        vm = new StaffAccountViewModel();
        DataContext = vm;

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.NewPassword) && vm.NewPassword.Length == 0)
                PasswordInput.Clear();
        };
        Loaded += async (_, _) => await vm.ReloadAsync();
    }

    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is StaffAccountViewModel vm)
            vm.NewPassword = PasswordInput.Password;
    }
}