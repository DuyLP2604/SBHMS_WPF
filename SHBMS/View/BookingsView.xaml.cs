using SHBMS.ViewModel;
using System.Windows.Controls;
namespace SHBMS.View;
public partial class BookingsView : UserControl
{
    private readonly BookingsViewModel _vm = new();
    public event Action? NewBookingRequested;
    public BookingsView()
    {
        InitializeComponent(); DataContext = _vm;
        _vm.NewBookingRequested += () => NewBookingRequested?.Invoke();
        Loaded += async (_, _) => await _vm.ReloadAsync();
    }
    public void ShowCreatedBooking(string id) => _vm.ShowCreatedBooking(id);
}
