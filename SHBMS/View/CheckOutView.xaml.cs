using SHBMS.ViewModel;
using System.Windows.Controls;

namespace SHBMS.View;

public partial class CheckOutView : UserControl
{
    public CheckOutView()
    {
        InitializeComponent();
        var vm = new CheckOutViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
