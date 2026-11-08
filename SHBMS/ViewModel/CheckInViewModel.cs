using SHBMS.DTO.Booking;
using SHBMS.DTO.Stay;
using SHBMS.Helper;
using SHBMS.Service.Implementation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace SHBMS.ViewModel;

public class CheckInViewModel : DatabaseViewModel
{
    private readonly BookingService _bookings = new();
    private readonly StayWorkflowService _stays = new();
    public ObservableCollection<BookingDto> Bookings { get; } = new();

    // Each row has its own selected room ID, even when several rows use the same type.
    public ObservableCollection<RoomAssignmentViewModel> RoomAssignments { get; } = new();
    public ICollectionView BookingsView { get; }
    public bool CanOperate => Session.GetRole() == "Staff";
    public string Summary =>
        $"{Bookings.Count} confirmed bookings | Overdue: {Bookings.Count(b => b.IsCheckInOverdue)}";
    public string SelectionHint => SelectedBooking == null ? "Select a booking to see its available actions."
        : SelectedBooking.IsCheckInOverdue ? "Check-in date passed. You can cancel this booking as a no-show."
        : !SelectedBooking.CanCheckIn ? "This booking has not reached its check-in date."
        : "Ready to check in today. Assign rooms and collect the full remaining room charge.";

    // Collect only the unpaid room charge here. Service charges are settled at checkout.
    public decimal AmountDue => Math.Max(
        0,
        (SelectedBooking?.RoomSubtotal ?? 0) - (SelectedBooking?.PaidAmount ?? 0)
    );
    public bool HasAmountDue => AmountDue > 0;

    // This is staff confirmation of cash received, not a payment saved by itself.
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

    // Keep the cancellation target separate from the normal assignment form.
    private BookingDto? _bookingToCancel;
    public BookingDto? BookingToCancel
    {
        get => _bookingToCancel;
        private set
        {
            SetProperty(ref _bookingToCancel, value);
            OnPropertyChanged(nameof(IsCancelling));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsCancelling => BookingToCancel != null;

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
            if (SetProperty(ref _selectedBooking, value))
            {
                CloseForm();
                BookingToCancel = null;
                OnPropertyChanged(nameof(SelectionHint));
                OnPropertyChanged(nameof(AmountDue));
                OnPropertyChanged(nameof(HasAmountDue));
            }
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool _isAssigning;
    public bool IsAssigning
    {
        get => _isAssigning;
        private set => SetProperty(ref _isAssigning, value);
    }

    // Remember which booking opened the form so assignments are submitted for that booking.
    private string? _formBookingId;

    public RelayCommand OpenAssignCommand { get; }
    public RelayCommand CancelAssignCommand { get; }
    public RelayCommand CheckInCommand { get; }
    public RelayCommand CancelNoShowCommand { get; }
    public RelayCommand ConfirmCancelCommand { get; }
    public RelayCommand KeepBookingCommand { get; }

    /// <summary>
    /// Prepare arrival search, room assignment and no-show cancellation commands.
    /// </summary>
    public CheckInViewModel()
    {
        BookingsView = CollectionViewSource.GetDefaultView(Bookings);
        BookingsView.Filter = item => item is BookingDto b && (string.IsNullOrWhiteSpace(SearchText)
                || Contains(b.BookingId, SearchText.Trim()) || Contains(b.CustomerName, SearchText.Trim())
                || Contains(b.CustomerPhone, SearchText.Trim()));
        OpenAssignCommand = DatabaseCommand(
            OpenFormAsync,
            () => CanOperate && SelectedBooking?.CanCheckIn == true && !IsAssigning && !IsCancelling
        );
        CancelAssignCommand = new RelayCommand(_ => CloseForm(), _ => !IsBusy);
        // Allow submission only after every room is selected and any room balance is received.
        CheckInCommand = DatabaseCommand(
            CheckInAsync,
            () => CanOperate && SelectedBooking?.CanCheckIn == true && IsAssigning
                && RoomAssignments.Count > 0 && RoomAssignments.All(r => !string.IsNullOrEmpty(r.SelectedRoomId))
                && (AmountDue == 0 || CashReceived)
        );
        CancelNoShowCommand = new RelayCommand(item =>
        {
            if (item is not BookingDto booking)
                return;
            SelectedBooking = booking;
            CloseForm();
            BookingToCancel = booking;
        }, item => CanOperate && !IsBusy && item is BookingDto booking && booking.IsCheckInOverdue);
        ConfirmCancelCommand = DatabaseCommand(
            CancelNoShowAsync,
            () => CanOperate && BookingToCancel?.IsCheckInOverdue == true
        );
        KeepBookingCommand = new RelayCommand(_ => BookingToCancel = null, _ => !IsBusy);
    }

    /// <summary>
    /// Reload confirmed bookings and place overdue arrivals first.
    /// </summary>
    protected override async Task LoadDataAsync()
    {
        CloseForm();
        BookingToCancel = null;
        SelectedBooking = null;
        var data = await _bookings.GetAllBookingsAsync();
        Bookings.Clear();
        foreach (var b in data
                .Where(b => b.Status == "CONFIRMED")
                .OrderByDescending(b => b.IsCheckInOverdue)
                .ThenBy(b => b.CheckInDate))
            Bookings.Add(b);
        OnPropertyChanged(nameof(Summary));
    }

    /// <summary>
    /// Create one room selection row for every room requested by the booking.
    /// </summary>
    private async Task OpenFormAsync()
    {
        if (SelectedBooking == null)
            return;
        var booking = SelectedBooking;
        CloseForm();
        var rooms = await _stays.GetAvailableRoomsAsync(booking.BookingId);
        // A booking can contain several room types, with several rooms of each type.
        foreach (var detail in booking.Rooms)
        {
            var choices = rooms.Where(r => r.RoomTypeId == detail.RoomTypeId).ToList();
            for (int i = 1; i <= detail.Quantity; i++)
            {
                var row = new RoomAssignmentViewModel
                {
                    RoomTypeId = detail.RoomTypeId,
                    Label = $"{detail.TypeName} - Room {i}",
                    AvailableRooms = choices
                };
                // Update command availability whenever a room dropdown changes.
                row.PropertyChanged += RoomSelectionChanged;
                RoomAssignments.Add(row);
            }
        }
        _formBookingId = booking.BookingId;
        IsAssigning = true;
        Notify(
            "Choose a different room for each row and collect the remaining room charge before checking in.",
            "info"
        );
    }

    /// <summary>
    /// Recheck the Check in button after a room selection changes.
    /// </summary>
    private void RoomSelectionChanged(object? sender, PropertyChangedEventArgs e)
        => CommandManager.InvalidateRequerySuggested();

    /// <summary>
    /// Clear draft assignments, payment confirmation and selection event handlers.
    /// </summary>
    private void CloseForm()
    {
        IsAssigning = false;
        _formBookingId = null;
        CashReceived = false;
        // Remove handlers before clearing rows to avoid keeping old form objects attached.
        foreach (var row in RoomAssignments)
            row.PropertyChanged -= RoomSelectionChanged;
        RoomAssignments.Clear();
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// Cancel the selected overdue arrival while preserving its history.
    /// </summary>
    private async Task CancelNoShowAsync()
    {
        if (BookingToCancel == null)
            return;
        string id = BookingToCancel.BookingId;
        await _stays.CancelNoShowAsync(id);
        await LoadDataAsync();
        Notify($"Booking {id} cancelled as a no-show. Its history and payments are kept.", "success");
    }

    /// <summary>
    /// Submit selected rooms and the displayed room balance for final validation.
    /// </summary>
    private async Task CheckInAsync()
    {
        if (_formBookingId == null || SelectedBooking == null)
            return;
        string id = _formBookingId;
        var selected = RoomAssignments.Select(r => new RoomSelectionDto
        {
            RoomTypeId = r.RoomTypeId,
            RoomId = r.SelectedRoomId ?? ""
        }).ToList();
        await _stays.CheckInAsync(
            id,
            selected,
            SelectedBooking.RoomSubtotal,
            SelectedBooking.PaidAmount,
            CashReceived
        );
        await LoadDataAsync();
        Notify($"Booking {id} checked in successfully.", "success");
    }
}
