using SHBMS.ViewModel;
using System.Windows.Controls;
namespace SHBMS.View;
public partial class RoomsView : UserControl
{
    public RoomsView()
    {
        InitializeComponent();
        var vm = new RoomsViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
