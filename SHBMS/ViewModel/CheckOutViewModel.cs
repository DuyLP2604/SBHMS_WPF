using SHBMS.DTO.Booking;
using SHBMS.Helper;
using SHBMS.Service.Implementation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace SHBMS.ViewModel;

public class CheckOutViewModel : DatabaseViewModel
{
    private readonly BookingService _bookings = new();
    private readonly StayWorkflowService _stays = new();
    public ObservableCollection<BookingDto> Bookings { get; } = new();
    public ICollectionView BookingsView { get; }
    public bool CanOperate => Session.GetRole() == "Staff";
    public bool HasSelection => SelectedBooking != null;
    public string Summary => $"{Bookings.Count} checked-in bookings";
    public string RoomText =>
        SelectedBooking == null
        ? ""
        : string.Join(
        ", ",
        SelectedBooking.Rooms
            .Select(r => r.AssignedRooms)
        );

    // Never ask staff to collect a negative balance.
    public decimal AmountDue => Math.Max(0, SelectedBooking?.RemainingAmount ?? 0);

    // Unfinished service orders do not block checkout; the service rejects them on completion.
    public bool CanCompleteCheckout => SelectedBooking != null;
    public string ServiceRequestHint =>
        SelectedBooking == null
        ? ""
        : SelectedBooking.OpenServiceRequestCount > 0
        ? $"{SelectedBooking.OpenServiceRequestCount} unfinished service order(s) will be rejected at checkout without a charge."
        : "Only completed and previously recorded services are charged.";

    // An overpayment is shown for separate settlement; checkout does not refund it.
    public decimal CreditBalance => Math.Max(0, -(SelectedBooking?.RemainingAmount ?? 0));

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            BookingsView.Refresh();
        }
    }

    private BookingDto? _selectedBooking;
    public BookingDto? SelectedBooking
    {
        get => _selectedBooking;
        set
        {
            SetProperty(ref _selectedBooking, value);
            // Selecting another booking requires a new confirmation of cash received.
            CashReceived = false;
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(RoomText));
            OnPropertyChanged(nameof(AmountDue));
            OnPropertyChanged(nameof(CreditBalance));
            OnPropertyChanged(nameof(CanCompleteCheckout));
            OnPropertyChanged(nameof(ServiceRequestHint));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // Checking this flag alone does not save a payment; Confirm check-out does.
    private bool _cashReceived;
    public bool CashReceived
    {
        get => _cashReceived;
        set
        {
            SetProperty(ref _cashReceived, value);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public RelayCommand CheckOutCommand { get; }

    /// <summary>
    /// Prepare in-house booking search and cash confirmation for checkout.
    /// </summary>
    public CheckOutViewModel()
    {
        BookingsView = CollectionViewSource.GetDefaultView(Bookings);
        BookingsView.Filter = item => item is BookingDto b && (string.IsNullOrWhiteSpace(SearchText)
                || Contains(b.BookingId, SearchText.Trim()) || Contains(b.CustomerName, SearchText.Trim())
                || Contains(b.CustomerPhone, SearchText.Trim()) || b.Rooms.Any(r => Contains(
                r.AssignedRooms,
                SearchText.Trim()
                )));
        // Cash confirmation is required only when the displayed balance is positive.
        CheckOutCommand = DatabaseCommand(
            CheckOutAsync,
            () => CanOperate && CanCompleteCheckout && (AmountDue == 0 || CashReceived)
        );
    }

    /// <summary>
    /// Reload checked-in bookings and order them by departure date.
    /// </summary>
    protected override async Task LoadDataAsync()
    {
        SelectedBooking = null;
        var data = await _bookings.GetAllBookingsAsync();
        Bookings.Clear();
        foreach (var b in data.Where(b => b.Status == "CHECKED_IN").OrderBy(b => b.CheckOutDate))
            Bookings.Add(b);
        OnPropertyChanged(nameof(Summary));
    }

    /// <summary>
    /// Complete checkout using the displayed totals and show the new invoice ID.
    /// </summary>
    private async Task CheckOutAsync()
    {
        if (SelectedBooking == null)
            return;
        var booking = SelectedBooking;
        string invoiceId = await _stays.CheckOutAsync(
            booking.BookingId,
            booking.TotalAmount,
            booking.PaidAmount,
            CashReceived
        );
        await LoadDataAsync();
        Notify(
            $"Booking {booking.BookingId} checked out. Invoice {invoiceId} created; rooms are available again.",
            "success"
        );
    }
}
