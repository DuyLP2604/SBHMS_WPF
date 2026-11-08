using Microsoft.Win32;
using SHBMS.DTO.Room;
using SHBMS.Helper;
using SHBMS.Service.Implementation;
using SHBMS.Service.Interface;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
namespace SHBMS.ViewModel;
public class RoomsViewModel : DatabaseViewModel
{
    private readonly IRoomService _service;
    public ObservableCollection<RoomDto> Rooms { get; } = new();
    public ObservableCollection<RoomTypeDto> RoomTypes { get; } = new();
    public ObservableCollection<RoomTypeDto> FilterRoomTypes { get; } = new();
    public ICollectionView RoomsView { get; }
    public RoomStatusEnum[] Statuses { get; } = Enum.GetValues<RoomStatusEnum>();
    public string[] FilterStatuses { get; } = { "All statuses", "Available", "Occupied", "Maintenance", "Inactive" };
    public bool CanView => Session.CanViewRooms;
    public bool CanManage => Session.IsManager;
    public bool IsReadOnly => !CanManage;
    public string AccessHint => CanManage ? "Manager access: add, update and delete rooms." : "View access: search, filter and select a room to see its details.";
    public string FormTitle => CanManage && SelectedRoom == null ? "New room" : "Room details";
    public string Summary => $"{RoomsView.Cast<object>().Count()} of {Rooms.Count} rooms | Available: {Rooms.Count(r => r.DisplayStatus == "Available")} | Occupied: {Rooms.Count(r => r.IsOccupied)}";
    private string _roomId = "";
    public string RoomId { get => _roomId; set => SetProperty(ref _roomId, value); }
    private string _roomNumber = "";
    public string RoomNumber { get => _roomNumber; set => SetProperty(ref _roomNumber, value); }
    private string _searchText = "";
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    private string _selectedRoomTypeId = "";
    public string SelectedRoomTypeId
    {
        get => _selectedRoomTypeId;
        set { SetProperty(ref _selectedRoomTypeId, value); OnPropertyChanged(nameof(NightlyPrice)); }
    }
    public decimal NightlyPrice => RoomTypes.FirstOrDefault(t => t.RoomTypeId == SelectedRoomTypeId)?.BasePrice ?? 0;
    private RoomStatusEnum _selectedStatus = RoomStatusEnum.Active;
    public RoomStatusEnum SelectedStatus { get => _selectedStatus; set => SetProperty(ref _selectedStatus, value); }
    private string _filterRoomTypeId = "";
    public string FilterRoomTypeId { get => _filterRoomTypeId; set { SetProperty(ref _filterRoomTypeId, value); RoomsView.Refresh(); } }
    private string _filterStatus = "All statuses";
    public string FilterStatus { get => _filterStatus; set { SetProperty(ref _filterStatus, value); RoomsView.Refresh(); } }
    private string? _imagePath;
    public string? ImagePath
    {
        get => _imagePath;
        set { SetProperty(ref _imagePath, value); OnPropertyChanged(nameof(ImagePreview)); OnPropertyChanged(nameof(HasImage)); }
    }
    public string? ImagePreview => RoomImageStorage.Resolve(ImagePath);
    public bool HasImage => ImagePreview != null;
    private RoomDto? _selectedRoom;
    public RoomDto? SelectedRoom
    {
        get => _selectedRoom;
        set
        {
            SetProperty(ref _selectedRoom, value);
            RoomId = value?.RoomId ?? "(auto)"; RoomNumber = value?.RoomNumber ?? "";
            SelectedRoomTypeId = value?.RoomTypeId ?? "";
            SelectedStatus = value?.Status ?? RoomStatusEnum.Active; ImagePath = value?.RoomImage;
            OnPropertyChanged(nameof(FormTitle));
        }
    }
    public RelayCommand NewCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand UpdateCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ResetFiltersCommand { get; }
    public RelayCommand ChooseImageCommand { get; }
    public RelayCommand ClearImageCommand { get; }
    public RoomsViewModel(IRoomService? service = null)
    {
        _service = service ?? new RoomService();
        RoomsView = CollectionViewSource.GetDefaultView(Rooms);
        RoomsView.Filter = item => item is RoomDto r
            && (string.IsNullOrEmpty(FilterRoomTypeId) || r.RoomTypeId == FilterRoomTypeId)
            && (FilterStatus == "All statuses" || r.DisplayStatus == FilterStatus)
            && (string.IsNullOrWhiteSpace(SearchText) || Contains(r.RoomId, SearchText.Trim())
                || Contains(r.RoomNumber, SearchText.Trim()) || Contains(r.RoomTypeName, SearchText.Trim())
                || Contains(r.CurrentGuestName, SearchText.Trim()));
        RoomsView.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Summary));
        NewCommand = new RelayCommand(_ => SelectedRoom = null, _ => CanManage && !IsBusy);
        AddCommand = DatabaseCommand(AddAsync, () => CanManage && SelectedRoom == null);
        UpdateCommand = DatabaseCommand(UpdateAsync, () => CanManage && SelectedRoom != null);
        DeleteCommand = DatabaseCommand(DeleteAsync, () => CanManage && SelectedRoom != null);
        SearchCommand = new RelayCommand(_ => RoomsView.Refresh(), _ => !IsBusy);
        ResetFiltersCommand = new RelayCommand(_ => { SearchText = ""; FilterRoomTypeId = ""; FilterStatus = "All statuses"; RoomsView.Refresh(); }, _ => !IsBusy);
        ChooseImageCommand = new RelayCommand(_ => ChooseImage(), _ => CanManage && !IsBusy);
        ClearImageCommand = new RelayCommand(_ => ImagePath = null, _ => CanManage && !IsBusy && ImagePath != null);
        SelectedRoom = null;
    }
    protected override async Task LoadDataAsync()
    {
        if (!CanView)
        {
            Rooms.Clear(); RoomTypes.Clear(); FilterRoomTypes.Clear(); SelectedRoom = null;
            throw new UnauthorizedAccessException("Only Staff and Manager can view rooms.");
        }
        var types = (await _service.GetAllRoomTypesAsync()).ToList();
        var rooms = (await _service.GetAllRoomsAsync()).ToList();
        RoomTypes.Clear(); FilterRoomTypes.Clear(); FilterRoomTypes.Add(new RoomTypeDto { Name = "All types" });
        foreach (var type in types) { RoomTypes.Add(type); FilterRoomTypes.Add(type); }
        if (!types.Any(t => t.RoomTypeId == FilterRoomTypeId)) FilterRoomTypeId = "";
        // Force the filter ComboBox to reselect the matching value after its items reload.
        OnPropertyChanged(nameof(FilterRoomTypeId));
        Rooms.Clear(); foreach (var room in rooms) Rooms.Add(room);
        SelectedRoom = CanManage ? null : Rooms.FirstOrDefault(); OnPropertyChanged(nameof(Summary));
    }
    private RoomCreateDto FormData() => new()
    { RoomNumber = RoomNumber, RoomTypeId = SelectedRoomTypeId, Status = SelectedStatus, RoomImage = ImagePath };
    private async Task AddAsync()
    {
        if (!CanManage) throw new UnauthorizedAccessException("Only Manager can add rooms.");
        await _service.CreateRoomAsync(FormData());
        await LoadDataAsync(); StatusMessage = "Room added.";
    }
    private async Task UpdateAsync()
    {
        if (!CanManage) throw new UnauthorizedAccessException("Only Manager can update rooms.");
        if (SelectedRoom == null) return;
        await _service.UpdateRoomAsync(new RoomUpdateDto
        { RoomId = SelectedRoom.RoomId, RoomNumber = RoomNumber, RoomTypeId = SelectedRoomTypeId, Status = SelectedStatus, RoomImage = ImagePath });
        await LoadDataAsync(); StatusMessage = "Room updated.";
    }
    private async Task DeleteAsync()
    {
        if (!CanManage) throw new UnauthorizedAccessException("Only Manager can delete rooms.");
        if (SelectedRoom == null) return;
        if (MessageBox.Show($"Delete room {SelectedRoom.RoomNumber}?", "Delete room", MessageBoxButton.YesNo,
            MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        await _service.DeleteRoomAsync(SelectedRoom.RoomId);
        await LoadDataAsync(); StatusMessage = "Room deleted.";
    }
    private void ChooseImage()
    {
        if (!CanManage) return;
        var dialog = new OpenFileDialog { Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp" };
        if (dialog.ShowDialog() != true) return;
        try { ImagePath = RoomImageStorage.Import(dialog.FileName, "rooms"); }
        catch (Exception ex) { StatusMessage = ex.Message; }
    }
}
