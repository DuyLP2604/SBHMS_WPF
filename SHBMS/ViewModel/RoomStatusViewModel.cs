using SHBMS.DTO.Room;
using SHBMS.Helper;
using SHBMS.Service.Implementation;
using SHBMS.Service.Interface;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
namespace SHBMS.ViewModel;

// FE-07 Monitor Room Status. All data comes from SQL Server via IRoomStatusService.
// Staff can change the operational status; Manager can only watch.
public class RoomStatusViewModel : DatabaseViewModel
{
    private readonly IRoomStatusService _service;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };
    private bool _quietRefreshing;
    private bool _quietFailureShown;

    public ObservableCollection<RoomStatusDto> Rooms { get; } = new();
    public ICollectionView RoomsView { get; }
    public string[] FilterStatuses { get; } = { "All", "Available", "Occupied", "Reserved", "Maintenance", "Inactive" };
    public RoomStatusEnum[] Statuses { get; } = Enum.GetValues<RoomStatusEnum>();

    public bool CanView => Session.CanViewRooms;
    public bool CanUpdate => Session.GetRole() == "Staff";
    public string AccessHint => CanUpdate
        ? "Staff access: select a room and change its operational status."
        : "View access: the board refreshes automatically every 30 seconds.";

    public int TotalCount => Rooms.Count;
    public int AvailableCount => Rooms.Count(r => r.DisplayStatus == "Available");
    public int OccupiedCount => Rooms.Count(r => r.DisplayStatus == "Occupied");
    public int ReservedCount => Rooms.Count(r => r.DisplayStatus == "Reserved");
    public int MaintenanceCount => Rooms.Count(r => r.DisplayStatus == "Maintenance");
    public int InactiveCount => Rooms.Count(r => r.DisplayStatus == "Inactive");
    public string OccupancyText
    {
        get
        {
            int inService = Rooms.Count(r => r.Status == RoomStatusEnum.Active || r.IsOccupied);
            return inService == 0 ? "Occupancy: n/a"
                : $"Occupancy: {OccupiedCount * 100 / inService}% ({OccupiedCount}/{inService} rooms in service)";
        }
    }

    private string _searchText = "";
    public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); RoomsView.Refresh(); } }
    private string _filterStatus = "All";
    public string FilterStatus { get => _filterStatus; set { SetProperty(ref _filterStatus, value); RoomsView.Refresh(); } }

    private string _lastUpdated = "Not loaded yet";
    public string LastUpdated { get => _lastUpdated; private set => SetProperty(ref _lastUpdated, value); }

    private bool _autoRefresh = true;
    public bool AutoRefresh { get => _autoRefresh; set => SetProperty(ref _autoRefresh, value); }

    private RoomStatusDto? _selectedRoom;
    public RoomStatusDto? SelectedRoom
    {
        get => _selectedRoom;
        set
        {
            SetProperty(ref _selectedRoom, value);
            SelectedStatus = value?.Status ?? RoomStatusEnum.Active;
            OnPropertyChanged(nameof(HasSelection));
        }
    }
    public bool HasSelection => SelectedRoom != null;
    private RoomStatusEnum _selectedStatus = RoomStatusEnum.Active;
    public RoomStatusEnum SelectedStatus { get => _selectedStatus; set => SetProperty(ref _selectedStatus, value); }

    public RelayCommand ResetFiltersCommand { get; }
    public RelayCommand FilterByCommand { get; }
    public RelayCommand UpdateStatusCommand { get; }

    public RoomStatusViewModel(IRoomStatusService? service = null)
    {
        _service = service ?? new RoomStatusService();
        RoomsView = CollectionViewSource.GetDefaultView(Rooms);
        RoomsView.Filter = item => item is RoomStatusDto r
            && (FilterStatus == "All" || r.DisplayStatus == FilterStatus)
            && (string.IsNullOrWhiteSpace(SearchText) || Contains(r.RoomNumber, SearchText.Trim())
                || Contains(r.RoomTypeName, SearchText.Trim()) || Contains(r.CurrentGuestName, SearchText.Trim())
                || Contains(r.ReservedGuestName, SearchText.Trim()) || Contains(r.BookingId, SearchText.Trim()));
        ResetFiltersCommand = new RelayCommand(_ => { SearchText = ""; FilterStatus = "All"; }, _ => !IsBusy);
        FilterByCommand = new RelayCommand(p => FilterStatus = (p as string) ?? "All", _ => !IsBusy);
        UpdateStatusCommand = DatabaseCommand(UpdateStatusAsync,
            () => CanUpdate && SelectedRoom != null && SelectedRoom.Status != SelectedStatus);
        _timer.Tick += async (_, _) => await RefreshQuietlyAsync();
    }

    // The view starts the timer while it is on screen and stops it when the user leaves the page.
    public void StartAutoRefresh() => _timer.Start();
    public void StopAutoRefresh() => _timer.Stop();

    protected override async Task LoadDataAsync()
    {
        if (!CanView)
        {
            Rooms.Clear(); SelectedRoom = null; RaiseCounts();
            throw new UnauthorizedAccessException("Only Staff and Manager can view room status.");
        }
        var data = await _service.GetRoomStatusesAsync();
        string? keep = SelectedRoom?.RoomId;
        Rooms.Clear();
        foreach (var room in data) Rooms.Add(room);
        SelectedRoom = keep == null ? null : Rooms.FirstOrDefault(r => r.RoomId == keep);
        RaiseCounts();
        LastUpdated = $"Updated {DateTime.Now:HH:mm:ss}";
    }

    private async Task RefreshQuietlyAsync()
    {
        if (!AutoRefresh || IsBusy || _quietRefreshing) return;
        // Do not overwrite a status the user has picked but not yet applied.
        if (SelectedRoom != null && SelectedStatus != SelectedRoom.Status) return;
        _quietRefreshing = true;
        try { await LoadDataAsync(); _quietFailureShown = false; }
        catch (Exception ex)
        {
            // Show a failed background refresh once, not every 30 seconds.
            if (!_quietFailureShown) { _quietFailureShown = true; Notify(DatabaseErrorMessages.Describe(ex), "error"); }
        }
        finally { _quietRefreshing = false; }
    }

    private async Task UpdateStatusAsync()
    {
        if (!CanUpdate) throw new UnauthorizedAccessException("Only Staff can update room status.");
        var room = SelectedRoom;
        if (room == null) return;
        string number = room.RoomNumber;
        var target = SelectedStatus;
        await _service.UpdateRoomStatusAsync(room.RoomId, target);
        await LoadDataAsync();
        Notify($"Room {number} is now {target}.", "success");
    }

    private void RaiseCounts()
    {
        OnPropertyChanged(nameof(TotalCount)); OnPropertyChanged(nameof(AvailableCount));
        OnPropertyChanged(nameof(OccupiedCount)); OnPropertyChanged(nameof(ReservedCount));
        OnPropertyChanged(nameof(MaintenanceCount)); OnPropertyChanged(nameof(InactiveCount));
        OnPropertyChanged(nameof(OccupancyText));
    }
}
