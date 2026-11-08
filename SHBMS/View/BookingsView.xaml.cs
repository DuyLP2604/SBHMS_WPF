using SHBMS.ViewModel;
using System.Windows.Controls;

namespace SHBMS.View;

public partial class BookingsView : UserControl
{
    private readonly BookingsViewModel _vm = new();

    // Let the parent page handle navigation instead of putting it in the ViewModel.
    public event Action? NewBookingRequested;

    /// <summary>
    /// Initialize bindings and reload page data when the view is loaded.
    /// </summary>
    public BookingsView()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.NewBookingRequested += () => NewBookingRequested?.Invoke();
        Loaded += async (_, _) => await _vm.ReloadAsync();
    }

    /// <summary>
    /// Pass the new booking ID to the list filters and success message.
    /// </summary>
    public void ShowCreatedBooking(string id) => _vm.ShowCreatedBooking(id);
}
