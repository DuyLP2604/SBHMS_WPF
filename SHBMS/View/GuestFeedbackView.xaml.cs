using SHBMS.ViewModel;
using System.Windows.Controls;
namespace SHBMS.View;
public partial class GuestFeedbackView : UserControl
{
    public GuestFeedbackView()
    {
        InitializeComponent();
        var vm = new GuestFeedbackViewModel();
        DataContext = vm;
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
