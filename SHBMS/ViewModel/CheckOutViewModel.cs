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
    public string RoomText => SelectedBooking == null ? "" : string.Join(", ", SelectedBooking.Rooms.Select(r => r.AssignedRooms));
    public decimal AmountDue => Math.Max(0, SelectedBooking?.RemainingAmount ?? 0);
    public decimal CreditBalance => Math.Max(0, -(SelectedBooking?.RemainingAmount ?? 0));

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); BookingsView.Refresh(); }
    }
    private BookingDto? _selectedBooking;
    public BookingDto? SelectedBooking
    {
        get => _selectedBooking;
        set
        {
            SetProperty(ref _selectedBooking, value);
            CashReceived = false;
            OnPropertyChanged(nameof(HasSelection)); OnPropertyChanged(nameof(RoomText));
            OnPropertyChanged(nameof(AmountDue)); OnPropertyChanged(nameof(CreditBalance));
            CommandManager.InvalidateRequerySuggested();
        }
    }
    private bool _cashReceived;
    public bool CashReceived
    {
        get => _cashReceived;
        set { SetProperty(ref _cashReceived, value); CommandManager.InvalidateRequerySuggested(); }
    }
    public RelayCommand CheckOutCommand { get; }

    public CheckOutViewModel()
    {
        BookingsView = CollectionViewSource.GetDefaultView(Bookings);
        BookingsView.Filter = item => item is BookingDto b && (string.IsNullOrWhiteSpace(SearchText)
            || Contains(b.BookingId, SearchText.Trim()) || Contains(b.CustomerName, SearchText.Trim())
            || Contains(b.CustomerPhone, SearchText.Trim()) || b.Rooms.Any(r => Contains(r.AssignedRooms, SearchText.Trim())));
        CheckOutCommand = DatabaseCommand(CheckOutAsync, () => CanOperate && HasSelection && (AmountDue == 0 || CashReceived));
    }

    protected override async Task LoadDataAsync()
    {
        SelectedBooking = null;
        var data = await _bookings.GetAllBookingsAsync();
        Bookings.Clear();
        foreach (var b in data.Where(b => b.Status == "CHECKED_IN").OrderBy(b => b.CheckOutDate)) Bookings.Add(b);
        OnPropertyChanged(nameof(Summary));
    }

    private async Task CheckOutAsync()
    {
        if (SelectedBooking == null) return;
        var booking = SelectedBooking;
        string invoiceId = await _stays.CheckOutAsync(booking.BookingId, booking.TotalAmount, booking.PaidAmount, CashReceived);
        await LoadDataAsync();
        Notify($"Booking {booking.BookingId} checked out. Invoice {invoiceId} created; rooms are available again.", "success");
    }
}
