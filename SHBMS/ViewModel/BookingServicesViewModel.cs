using SHBMS.DTO.Service;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Implementation;
using System.Collections.ObjectModel;
using System.Windows.Input;
namespace SHBMS.ViewModel;

public class BookingServicesViewModel : DatabaseViewModel
{
    private readonly BookingServicesManager _manager = new();
    public ObservableCollection<GuestServiceRequest> Orders { get; } = new();
    public ObservableCollection<Booking> Bookings { get; } = new();
    public ObservableCollection<SHBMS.Model.Service> Services { get; } = new();
    public bool CanManage => Session.IsStaff;
    public bool IsNew => SelectedOrder == null;
    public bool CanEditOrder => CanManage && (IsNew || (SelectedOrder?.Status == "Pending" && !SelectedOrder.IsLegacy));
    public bool CanChooseService => CanManage && IsNew;
    public string CustomerName => SelectedOrder?.CustomerName ?? SelectedBooking?.Customer.FullName ?? "";
    public string SelectionHint => SelectedOrder == null ? "Place an order. It will wait for processing before any charge is added."
        : SelectedOrder.IsLegacy ? "Previously recorded service. Its historical charge is preserved."
        : SelectedOrder.Status == "Pending" ? "You can change the quantity or cancel this pending order."
        : "This order is read-only here. Process open orders on Service requests.";

    private GuestServiceRequest? _selectedOrder;
    public GuestServiceRequest? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            SetProperty(ref _selectedOrder, value);
            SelectedBooking = Bookings.FirstOrDefault(b => b.BookingId == value?.BookingId);
            SelectedService = Services.FirstOrDefault(s => s.ServiceId == value?.ServiceId);
            QuantityText = value?.Quantity.ToString() ?? "1";
            Details = value?.Details ?? "";
            OnPropertyChanged(nameof(IsNew)); OnPropertyChanged(nameof(CanEditOrder));
            OnPropertyChanged(nameof(CanChooseService)); OnPropertyChanged(nameof(CustomerName));
            OnPropertyChanged(nameof(SelectionHint));
            CommandManager.InvalidateRequerySuggested();
        }
    }
    private Booking? _selectedBooking;
    public Booking? SelectedBooking
    {
        get => _selectedBooking;
        set { SetProperty(ref _selectedBooking, value); OnPropertyChanged(nameof(CustomerName)); }
    }
    private SHBMS.Model.Service? _selectedService;
    public SHBMS.Model.Service? SelectedService { get => _selectedService; set => SetProperty(ref _selectedService, value); }
    private string _quantityText = "1";
    public string QuantityText { get => _quantityText; set => SetProperty(ref _quantityText, value); }
    private string _details = "";
    public string Details { get => _details; set => SetProperty(ref _details, value); }
    public RelayCommand NewCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelOrderCommand { get; }

    public BookingServicesViewModel()
    {
        NewCommand = new RelayCommand(_ => SelectedOrder = null, _ => !IsBusy && CanManage);
        SaveCommand = DatabaseCommand(SaveAsync, () => CanEditOrder);
        CancelOrderCommand = DatabaseCommand(CancelAsync, () => CanManage
            && SelectedOrder is { IsLegacy: false, Status: "Pending" });
    }

    protected override async Task LoadDataAsync()
    {
        var bookings = await _manager.GetBookingsAsync();
        var services = await _manager.GetServicesAsync();
        var orders = await _manager.GetOrdersAsync();
        SelectedOrder = null;
        Bookings.Clear(); foreach (var booking in bookings) Bookings.Add(booking);
        Services.Clear(); foreach (var service in services) Services.Add(service);
        Orders.Clear(); foreach (var order in orders) Orders.Add(order);
    }

    private async Task SaveAsync()
    {
        if (!int.TryParse(QuantityText, out int quantity) || quantity < 1)
            throw new ArgumentException("Quantity must be a positive whole number.");
        if (SelectedOrder == null)
        {
            if (SelectedBooking == null || SelectedService == null)
                throw new ArgumentException("Select a checked-in booking and a service.");
            await _manager.CreateAsync(SelectedBooking.BookingId, SelectedService.ServiceId, quantity, Details);
        }
        else
        {
            if (SelectedOrder.IsLegacy) throw new InvalidOperationException("Previously recorded charges cannot be edited here.");
            await _manager.UpdatePendingAsync(SelectedOrder.Id, quantity, Details, SelectedOrder.Quantity);
        }
        await LoadDataAsync();
        Notify("Service order saved. Pending orders are not charged until completed.", "success");
    }

    private async Task CancelAsync()
    {
        if (SelectedOrder == null || SelectedOrder.IsLegacy) return;
        await _manager.CancelPendingAsync(SelectedOrder.Id);
        await LoadDataAsync();
        Notify("Pending service order cancelled without a charge.", "success");
    }
}
