using SHBMS.ViewModel;
using System.Windows.Controls;

namespace SHBMS.View;

public partial class CheckOutView : UserControl
{
    /// <summary>
    /// Initialize bindings and reload page data when the view is loaded.
    /// </summary>
    public CheckOutView()
    {
        InitializeComponent();
        var vm = new CheckOutViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
