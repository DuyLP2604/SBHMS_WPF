using SHBMS.ViewModel;
using System.Windows.Controls;

namespace SHBMS.View;

public partial class RoomsView : UserControl
{
    /// <summary>
    /// Initialize bindings and reload page data when the view is loaded.
    /// </summary>
    public RoomsView()
    {
        InitializeComponent();
        var vm = new RoomsViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
