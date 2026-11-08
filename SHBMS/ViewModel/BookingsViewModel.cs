using SHBMS.DTO.Booking;
using SHBMS.Helper;
using SHBMS.Service.Interface;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace SHBMS.ViewModel;

public class BookingsViewModel : DatabaseViewModel
{
    private readonly IBookingService _service;

    // Show the creation message after the list reloads, so it is not cleared by refresh.
    private string? _nextNotice;
    public ObservableCollection<BookingDto> Bookings { get; } = new();

    // Search and filters affect this view, while Bookings keeps all loaded rows.
    public ICollectionView BookingsView { get; }
    public string[] Statuses { get; } =
    {
        "All statuses",
        "CONFIRMED",
        "CHECKED_IN",
        "CHECKED_OUT",
        "CANCELLED"
    };
    public bool CanView => Session.CanViewBookings;
    public bool CanCreate => Session.GetRole() == "Staff";
    public string AccessHint =>
        CanCreate
        ? "Staff access: view bookings or create a booking for a customer."
        : "View access: search, filter and select a booking to see its details.";
    public string Summary => $"{BookingsView.Cast<object>().Count()} of {Bookings.Count} bookings";
    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _filterStatus = "All statuses";
    public string FilterStatus
    {
        get => _filterStatus;
        set => SetProperty(ref _filterStatus, value);
    }

    // Both date filters refer to the check-in date, including their boundary dates.
    private DateTime? _fromDate;
    public DateTime? FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    private DateTime? _toDate;
    public DateTime? ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    private BookingDto? _selectedBooking;
    public BookingDto? SelectedBooking
    {
        get => _selectedBooking;
        set => SetProperty(ref _selectedBooking, value);
    }

    public RelayCommand SearchCommand { get; }
    public RelayCommand ResetFiltersCommand { get; }
    public RelayCommand NewBookingCommand { get; }

    // The View forwards navigation to its parent without opening a window here.
    public event Action? NewBookingRequested;

    /// <summary>
    /// Prepare booking filters and the event that opens the new booking page.
    /// </summary>
    public BookingsViewModel(IBookingService? service = null)
    {
        _service = service ?? new SHBMS.Service.Implementation.BookingService();
        BookingsView = CollectionViewSource.GetDefaultView(Bookings);
        BookingsView.Filter = item => item is BookingDto b
            && (FilterStatus == "All statuses" || b.Status == FilterStatus)
            && (!FromDate.HasValue || b.CheckInDate >= DateOnly.FromDateTime(FromDate.Value))
            && (!ToDate.HasValue || b.CheckInDate <= DateOnly.FromDateTime(ToDate.Value))
            && (string.IsNullOrWhiteSpace(SearchText) || Contains(b.BookingId, SearchText.Trim())
                || Contains(b.CustomerName, SearchText.Trim()) || Contains(b.CustomerPhone, SearchText.Trim()));
        BookingsView.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Summary));
        SearchCommand = new RelayCommand(_ => ApplyFilters(), _ => !IsBusy);
        ResetFiltersCommand = new RelayCommand(_ =>
        {
            SearchText = "";
            FilterStatus = "All statuses";
            FromDate = null;
            ToDate = null;
            ApplyFilters();
        }, _ => !IsBusy);
        NewBookingCommand = new RelayCommand(_ => NewBookingRequested?.Invoke(), _ => CanCreate && !IsBusy);
    }

    /// <summary>
    /// Focus the list on the new booking and queue its success message.
    /// </summary>
    public void ShowCreatedBooking(string id)
    {
        SearchText = id;
        FilterStatus = "All statuses";
        FromDate = null;
        ToDate = null;
        _nextNotice = "Booking " + id + " created successfully.";
    }

    /// <summary>
    /// Reload visible bookings and select the first matching row.
    /// </summary>
    protected override async Task LoadDataAsync()
    {
        if (!CanView)
        {
            Bookings.Clear();
            SelectedBooking = null;
            _nextNotice = null;
            throw new UnauthorizedAccessException("Only Staff and Manager can access bookings.");
        }
        var data = await _service.GetAllBookingsAsync();
        // After reloading, select from the filtered view rather than the unfiltered collection.
        Bookings.Clear();
        foreach (var booking in data)
            Bookings.Add(booking);
        BookingsView.Refresh();
        SelectedBooking = BookingsView.Cast<BookingDto>().FirstOrDefault();
        OnPropertyChanged(nameof(Summary));
        if (_nextNotice != null)
        {
            Notify(_nextNotice, "success");
            _nextNotice = null;
        }
    }

    /// <summary>
    /// Validate the arrival date range and refresh the booking list.
    /// </summary>
    private void ApplyFilters()
    {
        if (FromDate.HasValue && ToDate.HasValue && ToDate.Value.Date < FromDate.Value.Date)
        {
            Notify("The end of the check-in range must not be before its start.", "warning");
            return;
        }
        BookingsView.Refresh();
        SelectedBooking = BookingsView.Cast<BookingDto>().FirstOrDefault();
    }
}
