using SHBMS.ViewModel;
using System.Windows.Controls;

namespace SHBMS.View;

public partial class CreateBookingView : UserControl
{
    public event Action? BackRequested;

    // Forward the saved ID to the parent so it can return to the booking list.
    public event Action<string>? BookingCreated;

    /// <summary>
    /// Initialize bindings and reload page data when the view is loaded.
    /// </summary>
    public CreateBookingView()
    {
        InitializeComponent();
        var vm = new CreateBookingViewModel();
        DataContext = vm;
        vm.BackRequested += () => BackRequested?.Invoke();
        vm.BookingCreated += id => BookingCreated?.Invoke(id);
        // Load the nationality list when this page opens.
        Loaded += async (_, _) => await vm.ReloadAsync();
    }
}
