using SHBMS.ViewModel;
using System.Windows.Controls;
namespace SHBMS.View;
public partial class RoomStatusView : UserControl
{
    public RoomStatusView()
    {
        InitializeComponent();
        var vm = new RoomStatusViewModel();
        DataContext = vm;
        // Reload every time the page is shown; poll only while it is on screen.
        Loaded += async (_, _) => { await vm.ReloadAsync(); vm.StartAutoRefresh(); };
        Unloaded += (_, _) => vm.StopAutoRefresh();
    }
}
