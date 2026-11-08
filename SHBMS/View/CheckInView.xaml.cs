using SHBMS.ViewModel;
using System.Windows.Controls;

namespace SHBMS.View;

public partial class CheckInView : UserControl
{
    public CheckInView()
    {
        InitializeComponent();
        var vm = new CheckInViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
