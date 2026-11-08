using SHBMS.DTO.Booking;
using SHBMS.Helper;
using SHBMS.Service.Interface;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
namespace SHBMS.ViewModel;
public class BookingEmployeeOption
{
    public string EmployeeId { get; set; } = "";
    public string Name { get; set; } = "";
}
public class BookingsViewModel : DatabaseViewModel
{
    private readonly IBookingService _service;
    private string? _nextNotice;
    public ObservableCollection<BookingDto> Bookings { get; } = new();
    public ObservableCollection<BookingEmployeeOption> Employees { get; } = new();
    public ICollectionView BookingsView { get; }
    public string[] Statuses { get; } = { "All statuses", "PENDING", "CONFIRMED", "CHECKED_IN", "CHECKED_OUT", "CANCELLED" };
    public bool CanView => Session.CanViewBookings;
    public bool CanCreate => Session.GetRole() == "Staff";
    public string AccessHint => CanCreate ? "Staff access: view bookings or create a booking for a customer." : "View access: search, filter and select a booking to see its details.";
    public string Summary => $"{BookingsView.Cast<object>().Count()} of {Bookings.Count} bookings";
    private string _searchText = "";
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    private string _filterStatus = "All statuses";
    public string FilterStatus { get => _filterStatus; set => SetProperty(ref _filterStatus, value); }
    private string _filterEmployeeId = "";
    public string FilterEmployeeId { get => _filterEmployeeId; set => SetProperty(ref _filterEmployeeId, value); }
    private DateTime? _fromDate;
    public DateTime? FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
    private DateTime? _toDate;
    public DateTime? ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }
    private BookingDto? _selectedBooking;
    public BookingDto? SelectedBooking { get => _selectedBooking; set => SetProperty(ref _selectedBooking, value); }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ResetFiltersCommand { get; }
    public RelayCommand NewBookingCommand { get; }
    public event Action? NewBookingRequested;
    public BookingsViewModel(IBookingService? service = null)
    {
        _service = service ?? new SHBMS.Service.Implementation.BookingService();
        BookingsView = CollectionViewSource.GetDefaultView(Bookings);
        BookingsView.Filter = item => item is BookingDto b
            && (FilterStatus == "All statuses" || b.Status == FilterStatus)
            && (string.IsNullOrEmpty(FilterEmployeeId) || b.EmployeeId == FilterEmployeeId)
            && (!FromDate.HasValue || b.CheckInDate >= DateOnly.FromDateTime(FromDate.Value))
            && (!ToDate.HasValue || b.CheckInDate <= DateOnly.FromDateTime(ToDate.Value))
            && (string.IsNullOrWhiteSpace(SearchText) || Contains(b.BookingId, SearchText.Trim())
                || Contains(b.CustomerName, SearchText.Trim()) || Contains(b.CustomerPhone, SearchText.Trim()));
        BookingsView.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Summary));
        SearchCommand = new RelayCommand(_ => ApplyFilters(), _ => !IsBusy);
        ResetFiltersCommand = new RelayCommand(_ => { SearchText = ""; FilterStatus = "All statuses";
            FilterEmployeeId = ""; FromDate = null; ToDate = null; ApplyFilters(); }, _ => !IsBusy);
        NewBookingCommand = new RelayCommand(_ => NewBookingRequested?.Invoke(), _ => CanCreate && !IsBusy);
    }
    public void ShowCreatedBooking(string id)
    {
        SearchText = id; FilterStatus = "All statuses"; FilterEmployeeId = ""; FromDate = null; ToDate = null;
        _nextNotice = "Booking " + id + " created successfully.";
    }
    protected override async Task LoadDataAsync()
    {
        if (!CanView)
        {
            Bookings.Clear(); Employees.Clear(); SelectedBooking = null; _nextNotice = null;
            throw new UnauthorizedAccessException("Only Staff and Manager can access bookings.");
        }
        var data = await _service.GetAllBookingsAsync();
        string employee = FilterEmployeeId;
        Employees.Clear(); Employees.Add(new BookingEmployeeOption { Name = "All employees" });
        foreach (var row in data.GroupBy(b => b.EmployeeId).Select(g => g.First()).OrderBy(b => b.CreatedByEmployeeName))
            Employees.Add(new BookingEmployeeOption { EmployeeId = row.EmployeeId, Name = row.CreatedByEmployeeName + " (" + row.EmployeeId + ")" });
        FilterEmployeeId = Employees.Any(e => e.EmployeeId == employee) ? employee : "";
        OnPropertyChanged(nameof(FilterEmployeeId));
        Bookings.Clear(); foreach (var booking in data) Bookings.Add(booking);
        BookingsView.Refresh(); SelectedBooking = BookingsView.Cast<BookingDto>().FirstOrDefault();
        OnPropertyChanged(nameof(Summary));
        if (_nextNotice != null) { StatusMessage = _nextNotice; _nextNotice = null; }
    }
    private void ApplyFilters()
    {
        if (FromDate.HasValue && ToDate.HasValue && ToDate.Value.Date < FromDate.Value.Date)
        { StatusMessage = "The end of the check-in range must not be before its start."; return; }
        BookingsView.Refresh(); SelectedBooking = BookingsView.Cast<BookingDto>().FirstOrDefault(); StatusMessage = "";
    }
}
