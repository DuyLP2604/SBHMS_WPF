using SHBMS.DTO.Guest;
using SHBMS.Helper;
using SHBMS.Service.Implementation;
using SHBMS.Service.Interface;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
namespace SHBMS.ViewModel;

// FE-02 Manage Guest Stay. Stays, rooms and charges are read from SQL Server via IGuestStayService.
// Check-in and check-out live in their own screens (CheckInView / CheckOutView).
public class GuestStayViewModel : DatabaseViewModel
{
    private readonly IGuestStayService _service;
    public ObservableCollection<GuestStayDto> Stays { get; } = new();
    public ICollectionView StaysView { get; }
    public string[] FilterOptions { get; } =
        { "Current", "Arriving today", "In house", "Due out today", "Upcoming", "Checked out", "All" };

    private string _searchText = "";
    public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); RefreshList(); } }
    private string _filterOption = "Current";
    public string FilterOption { get => _filterOption; set { SetProperty(ref _filterOption, value); RefreshList(); } }

    private GuestStayDto? _selectedStay;
    public GuestStayDto? SelectedStay
    {
        get => _selectedStay;
        set { SetProperty(ref _selectedStay, value); OnPropertyChanged(nameof(HasSelection)); }
    }
    public bool HasSelection => SelectedStay != null;

    public string Summary
    {
        get
        {
            int inHouse = Stays.Count(s => s.Status == "CHECKED_IN");
            int arriving = Stays.Count(s => s.Phase == "Arriving today");
            int dueOut = Stays.Count(s => s.Phase is "Due out today" or "Overdue checkout");
            return $"In house: {inHouse} | Arriving today: {arriving} | Due out: {dueOut} | Showing {StaysView.Cast<object>().Count()} of {Stays.Count}";
        }
    }

    public RelayCommand SearchCommand { get; }

    public GuestStayViewModel(IGuestStayService? service = null)
    {
        _service = service ?? new GuestStayService();
        StaysView = CollectionViewSource.GetDefaultView(Stays);
        StaysView.Filter = item => item is GuestStayDto s && MatchesFilter(s) && (string.IsNullOrWhiteSpace(SearchText)
            || Contains(s.BookingId, SearchText.Trim()) || Contains(s.CustomerName, SearchText.Trim())
            || Contains(s.CustomerPhone, SearchText.Trim()) || Contains(s.RoomNumbers, SearchText.Trim())
            || Contains(s.Phase, SearchText.Trim()));
        SearchCommand = new RelayCommand(_ => RefreshList(), _ => !IsBusy);
    }

    private void RefreshList() { StaysView.Refresh(); OnPropertyChanged(nameof(Summary)); }

    private bool MatchesFilter(GuestStayDto s) => FilterOption switch
    {
        "Current" => s.Phase is "Arriving today" or "Overdue check-in" or "In house" or "Due out today" or "Overdue checkout",
        "Arriving today" => s.Phase == "Arriving today",
        "In house" => s.Status == "CHECKED_IN",
        "Due out today" => s.Phase is "Due out today" or "Overdue checkout",
        "Upcoming" => s.Phase == "Upcoming",
        "Checked out" => s.Phase == "Checked out",
        _ => true
    };

    protected override async Task LoadDataAsync()
    {
        if (!Session.CanViewBookings)
        {
            Stays.Clear(); SelectedStay = null;
            throw new UnauthorizedAccessException("Only Staff and Manager can view guest stays.");
        }
        var data = await _service.GetStaysAsync();
        string? keep = SelectedStay?.BookingId;
        Stays.Clear();
        foreach (var stay in data) Stays.Add(stay);
        SelectedStay = keep == null ? null : Stays.FirstOrDefault(s => s.BookingId == keep);
        OnPropertyChanged(nameof(Summary));
    }
}
