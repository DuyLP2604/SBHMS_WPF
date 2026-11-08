using SHBMS.ViewModel;
using System.Windows.Controls;

namespace SHBMS.View;

public partial class CheckInView : UserControl
{
    /// <summary>
    /// Initialize bindings and reload page data when the view is loaded.
    /// </summary>
    public CheckInView()
    {
        InitializeComponent();
        var vm = new CheckInViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
