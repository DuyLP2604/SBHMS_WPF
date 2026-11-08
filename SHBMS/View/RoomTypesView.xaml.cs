using SHBMS.ViewModel;
using System.Windows.Controls;
namespace SHBMS.View;
public partial class RoomTypesView : UserControl
{
    public RoomTypesView()
    {
        InitializeComponent();
        var vm = new RoomTypesViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
