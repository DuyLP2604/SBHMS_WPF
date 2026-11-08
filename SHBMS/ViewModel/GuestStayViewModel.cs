using Microsoft.EntityFrameworkCore;
using SHBMS.Helper;
using SHBMS.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
namespace SHBMS.ViewModel;
public class GuestStayViewModel : DatabaseViewModel
{
    public ObservableCollection<Booking> Bookings { get; } = new();
    public ICollectionView BookingsView { get; }
    private string _searchText = "";
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    private Booking? _selectedBooking;
    public Booking? SelectedBooking
    {
        get => _selectedBooking;
        set { SetProperty(ref _selectedBooking, value); OnPropertyChanged(nameof(RoomText)); OnPropertyChanged(nameof(TotalCharge)); }
    }
    public string Summary => $"In house: {Bookings.Count(b => b.BookingStatus == "CHECKED_IN")} | Arriving today: {Bookings.Count(b => b.CheckInDate == DateOnly.FromDateTime(DateTime.Today) && (b.BookingStatus == "PENDING" || b.BookingStatus == "CONFIRMED"))}";
    public string RoomText => SelectedBooking == null ? "" : string.Join(", ", SelectedBooking.BookingDetails
        .SelectMany(d => d.RoomAssignments).Select(a => a.Room.RoomNumber));
    public decimal TotalCharge => SelectedBooking == null ? 0 :
        SelectedBooking.BookingDetails.Sum(d => d.Quantity * d.UnitPrice)
        * (SelectedBooking.CheckOutDate.DayNumber - SelectedBooking.CheckInDate.DayNumber)
        + SelectedBooking.BookingServices.Sum(s => s.Quantity * s.UnitPrice);
    public RelayCommand SearchCommand { get; }
    public GuestStayViewModel()
    {
        BookingsView = CollectionViewSource.GetDefaultView(Bookings);
        BookingsView.Filter = item => item is Booking b && (string.IsNullOrWhiteSpace(SearchText)
            || Contains(b.BookingId, SearchText.Trim()) || Contains(b.Customer.FullName, SearchText.Trim())
            || Contains(b.BookingStatus, SearchText.Trim()));
        SearchCommand = new RelayCommand(_ => BookingsView.Refresh(), _ => !IsBusy);
    }
    protected override async Task LoadDataAsync()
    {
        using var db = new Prn212SbhmsContext();
        var data = await db.Bookings.AsNoTracking().Where(b => !b.IsDeleted).Include(b => b.Customer)
            .Include(b => b.BookingDetails).ThenInclude(d => d.RoomAssignments).ThenInclude(a => a.Room)
            .Include(b => b.BookingServices).OrderByDescending(b => b.CheckInDate).ToListAsync();
        Bookings.Clear(); foreach (var b in data) Bookings.Add(b);
        SelectedBooking = null; OnPropertyChanged(nameof(Summary));
        // The former GuestStay/HotelDemoStore workflow is not a database entity.
        // This migration provides a real booking overview without inventing stay records.
    }
}
