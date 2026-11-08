using SHBMS.ViewModel;
using System.Windows.Controls;
namespace SHBMS.View;
public partial class InvoiceManager : UserControl
{
    public InvoiceManager()
    {
        InitializeComponent();
        var vm = new InvoiceViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
