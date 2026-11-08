using Microsoft.EntityFrameworkCore;
using SHBMS.Helper;
using SHBMS.Model;
using System.Collections.ObjectModel;
using System.Windows;
namespace SHBMS.ViewModel;
public class ServiceRequestsViewModel : DatabaseViewModel
{
    public ObservableCollection<SHBMS.Model.BookingService> Orders { get; } = new();
    public ObservableCollection<Booking> Bookings { get; } = new();
    public ObservableCollection<SHBMS.Model.Service> Services { get; } = new();
    private SHBMS.Model.BookingService? _selectedOrder;
    public SHBMS.Model.BookingService? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            SetProperty(ref _selectedOrder, value);
            SelectedBooking = Bookings.FirstOrDefault(b => b.BookingId == value?.BookingId);
            SelectedService = Services.FirstOrDefault(s => s.ServiceId == value?.ServiceId);
            QuantityText = value?.Quantity.ToString() ?? "1";
            OnPropertyChanged(nameof(IsNew));
        }
    }
    private Booking? _selectedBooking;
    public Booking? SelectedBooking { get => _selectedBooking; set => SetProperty(ref _selectedBooking, value); }
    private SHBMS.Model.Service? _selectedService;
    public SHBMS.Model.Service? SelectedService { get => _selectedService; set => SetProperty(ref _selectedService, value); }
    private string _quantityText = "1";
    public string QuantityText { get => _quantityText; set => SetProperty(ref _quantityText, value); }
    public bool IsNew => SelectedOrder == null;
    public RelayCommand NewCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public ServiceRequestsViewModel()
    {
        NewCommand = new RelayCommand(_ => SelectedOrder = null, _ => !IsBusy);
        SaveCommand = DatabaseCommand(SaveAsync);
        DeleteCommand = DatabaseCommand(DeleteAsync, () => SelectedOrder != null);
    }
    protected override async Task LoadDataAsync()
    {
        using var db = new Prn212SbhmsContext();
        var bookings = await db.Bookings.AsNoTracking().Where(b => !b.IsDeleted && b.BookingStatus == "CHECKED_IN").OrderBy(b => b.BookingId).ToListAsync();
        var services = await db.Services.AsNoTracking().OrderBy(s => s.ServiceName).ToListAsync();
        var orders = await db.BookingServices.AsNoTracking().Include(o => o.Service).Include(o => o.Booking)
            .Where(o => !o.Booking.IsDeleted).OrderBy(o => o.BookingId).ToListAsync();
        Bookings.Clear(); foreach (var b in bookings) Bookings.Add(b);
        Services.Clear(); foreach (var s in services) Services.Add(s);
        Orders.Clear(); foreach (var o in orders) Orders.Add(o);
        SelectedOrder = null;
    }
    private async Task SaveAsync()
    {
        string? bookingId = SelectedOrder?.BookingId ?? SelectedBooking?.BookingId;
        string? serviceId = SelectedOrder?.ServiceId ?? SelectedService?.ServiceId;
        string? employeeId = Session.CurrentAccount?.EmployeeId;
        if (bookingId == null || serviceId == null || employeeId == null
            || !int.TryParse(QuantityText, out int quantity) || quantity < 1)
        { StatusMessage = "Select a booking and service, enter a positive quantity, and use a linked employee account."; return; }
        using var db = new Prn212SbhmsContext();
        var booking = await db.Bookings.FindAsync(bookingId);
        if (booking == null || booking.IsDeleted || booking.BookingStatus != "CHECKED_IN")
        { StatusMessage = "Service orders can only be edited for checked-in bookings."; return; }
        var service = await db.Services.FindAsync(serviceId);
        if (service == null) { StatusMessage = "Service no longer exists."; return; }
        var order = await db.BookingServices.FindAsync(bookingId, serviceId);
        if (IsNew && order != null) { StatusMessage = "This service is already ordered. Select its row to update the quantity."; return; }
        if (!IsNew && order == null) { StatusMessage = "Order no longer exists. Refresh the list."; return; }
        if (order == null)
        {
            order = new SHBMS.Model.BookingService { BookingId = bookingId, ServiceId = serviceId,
                UnitPrice = service.UnitPrice, OrderedByEmployee = employeeId };
            db.BookingServices.Add(order);
        }
        order.Quantity = quantity;
        await db.SaveChangesAsync(); await LoadDataAsync(); StatusMessage = "Service order saved to database.";
    }
    private async Task DeleteAsync()
    {
        if (SelectedOrder == null) return;
        if (MessageBox.Show("Remove the selected service order?", "Remove service", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        using var db = new Prn212SbhmsContext();
        var order = await db.BookingServices.Include(o => o.Booking).FirstOrDefaultAsync(o =>
            o.BookingId == SelectedOrder.BookingId && o.ServiceId == SelectedOrder.ServiceId);
        if (order == null) { StatusMessage = "Order no longer exists. Refresh the list."; return; }
        if (order.Booking.IsDeleted || order.Booking.BookingStatus != "CHECKED_IN")
        { StatusMessage = "Service orders can only be removed for checked-in bookings."; return; }
        db.BookingServices.Remove(order); await db.SaveChangesAsync(); await LoadDataAsync(); StatusMessage = "Service order removed.";
    }
}
