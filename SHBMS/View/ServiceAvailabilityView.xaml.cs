
using SHBMS.ViewModel;
using System.Windows.Controls;

namespace SHBMS.View;

public partial class ServiceAvailabilityView : UserControl
{
    public ServiceAvailabilityView()
    {
        InitializeComponent();

        var vm = new ServiceAvailabilityViewModel();

        DataContext = vm;

        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
