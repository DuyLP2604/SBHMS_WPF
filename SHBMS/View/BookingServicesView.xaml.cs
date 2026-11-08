using SHBMS.ViewModel;
using System.Windows.Controls;
namespace SHBMS.View;
public partial class BookingServicesView : UserControl
{
    public BookingServicesView()
    {
        InitializeComponent();
        var vm = new BookingServicesViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
