using SHBMS.ViewModel;
using System.Windows.Controls;
namespace SHBMS.View;
public partial class CustomerProfilesView : UserControl
{
    public CustomerProfilesView()
    {
        InitializeComponent();
        var vm = new CustomerProfilesViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
