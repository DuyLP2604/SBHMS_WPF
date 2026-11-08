using SHBMS.Helper;
using SHBMS.ViewModel;
using System.Windows;
using System.Windows.Controls;
namespace SHBMS.View;
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm = new();
    private readonly Dictionary<string, UserControl> _pages = new();
    public MainWindow()
    {
        InitializeComponent();

        // Initialize before creating or loading any page.
        ToastService.Initialize(
            ToastBorder,
            ToastTitle,
            ToastMessage);

        DataContext = _vm;

        Navigate(
            "Customers",
            "Customer profiles",
            CustomerNavButton,
            () => new CustomerProfilesView());

    }
    // Navigation is view behavior. Each page owns its own data ViewModel.
    // Create pages on demand so opening the shell does not query every database table.
    private void Navigate(string key, string title, Button button, Func<UserControl> create)
    {
        if ((key == "Rooms" || key == "RoomTypes" || key == "RoomStatus") && !_vm.CanViewRooms) return;
        if ((key == "Bookings" || key == "CheckIn" || key == "CheckOut") && !_vm.CanViewBookings) return;
        foreach (var nav in new[] { CustomerNavButton, BookingNavButton, CheckInNavButton, CheckOutNavButton, StayNavButton, RoomStatusNavButton, RoomNavButton, ServiceRequestNavButton,
            FeedbackNavButton, InvoiceNavButton, RoomTypeNavButton, StaffNavButton }) nav.Tag = null;
        button.Tag = "Selected";
        if (!_pages.TryGetValue(key, out var page)) { page = create(); _pages.Add(key, page); }
        PageHost.Content = page; _vm.PageTitle = title;
    }
    private void Customer_Click(object sender, RoutedEventArgs e) => Navigate("Customers", "Customer profiles", CustomerNavButton, () => new CustomerProfilesView());
    private void Feedback_Click(object sender, RoutedEventArgs e) => Navigate("Feedback", "Guest feedback", FeedbackNavButton, () => new GuestFeedbackView());
    private void Invoice_Click(object sender, RoutedEventArgs e) => Navigate("Invoices", "Invoices", InvoiceNavButton, () => new InvoiceManager());
    private void Bookings_Click(object sender, RoutedEventArgs e) => ShowBookings();
    private void ShowBookings(string? createdId = null)
    {
        if (!_vm.CanViewBookings) return;
        if (createdId != null && _pages.TryGetValue("Bookings", out var existing) && existing is BookingsView list)
            list.ShowCreatedBooking(createdId);
        Navigate("Bookings", "Bookings", BookingNavButton, () =>
        {
            var page = new BookingsView();
            page.NewBookingRequested += ShowCreateBooking;
            if (createdId != null) page.ShowCreatedBooking(createdId);
            return page;
        });
    }
    private void ShowCreateBooking()
    {
        if (Session.GetRole() != "Staff") return;
        var page = new CreateBookingView();
        page.BackRequested += () => ShowBookings();
        page.BookingCreated += id => ShowBookings(id);
        PageHost.Content = page; _vm.PageTitle = "New booking";
    }
    private void CheckIn_Click(object sender, RoutedEventArgs e)
        => Navigate("CheckIn", "Check-in", CheckInNavButton, () => new CheckInView());
    private void CheckOut_Click(object sender, RoutedEventArgs e)
        => Navigate("CheckOut", "Check-out", CheckOutNavButton, () => new CheckOutView());
    private void Stay_Click(object sender, RoutedEventArgs e) => Navigate("Stays", "Guest stays", StayNavButton, () => new GuestStayView());
    private void RoomStatus_Click(object sender, RoutedEventArgs e)
        => Navigate("RoomStatus", "Room status", RoomStatusNavButton, () => new RoomStatusView());
    private void Room_Click(object sender, RoutedEventArgs e) => Navigate("Rooms", "Rooms", RoomNavButton, () => new RoomsView());
    private void ServiceRequest_Click(object sender, RoutedEventArgs e) => Navigate("Services", "Booking services", ServiceRequestNavButton, () => new ServiceRequestsView());
    private void RoomTypes_Click(object sender, RoutedEventArgs e)
        => Navigate("RoomTypes", "Room types", RoomTypeNavButton, () => new RoomTypesView());
    private void Staff_Click(object sender, RoutedEventArgs e)
    {
        if (!_vm.IsAdmin) return;
        Navigate("Staff", "Staff accounts", StaffNavButton, () => new StaffAccountView());
    }
    private void SignOut_Click(object sender, RoutedEventArgs e)
    {
        Session.Clear();
        var login = new LoginView(); Application.Current.MainWindow = login; login.Show(); Close();
    }
}
