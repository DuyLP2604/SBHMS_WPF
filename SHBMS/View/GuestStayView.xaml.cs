using SHBMS.ViewModel;
using System.Windows.Controls;
namespace SHBMS.View;
public partial class GuestStayView : UserControl
{
    public GuestStayView()
    {
        InitializeComponent();
        var vm = new GuestStayViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
