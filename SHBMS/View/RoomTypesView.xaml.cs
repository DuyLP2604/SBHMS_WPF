using SHBMS.ViewModel;
using System.Windows.Controls;

namespace SHBMS.View;

public partial class RoomTypesView : UserControl
{
    /// <summary>
    /// Initialize bindings and reload page data when the view is loaded.
    /// </summary>
    public RoomTypesView()
    {
        InitializeComponent();
        var vm = new RoomTypesViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
