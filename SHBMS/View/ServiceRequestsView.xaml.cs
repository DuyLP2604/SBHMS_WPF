using System.Windows.Controls;
namespace SHBMS.View;
public partial class ServiceRequestsView : UserControl
{
    public ServiceRequestsView()
    {
        InitializeComponent();
        //var vm = new ServiceRequestsViewModel();
        //DataContext = vm;
        //Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
