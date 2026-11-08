using SHBMS.ViewModel;
using System.Windows.Controls;
namespace SHBMS.View;
public partial class CreateBookingView : UserControl
{
    public event Action? BackRequested;
    public event Action<string>? BookingCreated;
    public CreateBookingView()
    {
        InitializeComponent();
        var vm = new CreateBookingViewModel(); DataContext = vm;
        vm.BackRequested += () => BackRequested?.Invoke();
        vm.BookingCreated += id => BookingCreated?.Invoke(id);
    }
}
