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
public class RoomTypesViewModel : DatabaseViewModel
{
    private readonly IRoomService _service;
    public ObservableCollection<RoomTypeDto> RoomTypes { get; } = new();
    public ICollectionView RoomTypesView { get; }
    public bool CanView => Session.CanViewRooms;
    public bool CanManage => Session.IsManager;
    public bool IsReadOnly => !CanManage;
    public string AccessHint => CanManage ? "Manager access: update the four room types RT01 to RT04 and their nightly prices." : "View access: search, filter and select a room type to see its details.";
    public string FormTitle => CanManage && SelectedRoomType == null ? "New room type" : "Room type details";
    public string Summary => $"{RoomTypesView.Cast<object>().Count()} of {RoomTypes.Count} room types";
    private string _roomTypeId = "";
    public string RoomTypeId { get => _roomTypeId; set => SetProperty(ref _roomTypeId, value); }
    private string _typeName = "";
    public string TypeName { get => _typeName; set => SetProperty(ref _typeName, value); }
    private string _capacityText = "";
    public string CapacityText { get => _capacityText; set => SetProperty(ref _capacityText, value); }
    private string _priceText = "";
    public string PriceText { get => _priceText; set => SetProperty(ref _priceText, value); }
    private string _description = "";
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    private string _searchText = "";
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    private string _minimumCapacityText = "";
    public string MinimumCapacityText { get => _minimumCapacityText; set => SetProperty(ref _minimumCapacityText, value); }
    private string _maximumPriceText = "";
    public string MaximumPriceText { get => _maximumPriceText; set => SetProperty(ref _maximumPriceText, value); }
    private int? _minimumCapacity;
    private decimal? _maximumPrice;
    private string? _imagePath;
    public string? ImagePath
    {
        get => _imagePath;
        set { SetProperty(ref _imagePath, value); OnPropertyChanged(nameof(ImagePreview)); OnPropertyChanged(nameof(HasImage)); }
    }
    public string? ImagePreview => RoomImageStorage.Resolve(ImagePath);
    public bool HasImage => ImagePreview != null;
    private RoomTypeDto? _selectedRoomType;
    public RoomTypeDto? SelectedRoomType
    {
        get => _selectedRoomType;
        set
        {
            SetProperty(ref _selectedRoomType, value);
            RoomTypeId = value?.RoomTypeId ?? "(auto)"; TypeName = value?.Name ?? "";
            CapacityText = value?.Capacity.ToString() ?? ""; PriceText = value?.BasePrice.ToString() ?? "";
            Description = value?.Description ?? ""; ImagePath = value?.RoomTypeImage;
            OnPropertyChanged(nameof(FormTitle));
        }
    }
    public RelayCommand NewCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand UpdateCommand { get; }
    public RelayCommand DisableCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ResetFiltersCommand { get; }
    public RelayCommand ChooseImageCommand { get; }
    public RelayCommand ClearImageCommand { get; }
    public RoomTypesViewModel(IRoomService? service = null)
    {
        _service = service ?? new RoomService();
        RoomTypesView = CollectionViewSource.GetDefaultView(RoomTypes);
        RoomTypesView.Filter = item => item is RoomTypeDto r
            && (!_minimumCapacity.HasValue || r.Capacity >= _minimumCapacity)
            && (!_maximumPrice.HasValue || r.BasePrice <= _maximumPrice)
            && (string.IsNullOrWhiteSpace(SearchText) || Contains(r.RoomTypeId, SearchText.Trim())
                || Contains(r.Name, SearchText.Trim()) || Contains(r.Description, SearchText.Trim()));
        RoomTypesView.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Summary));
        NewCommand = new RelayCommand(_ => SelectedRoomType = null, _ => CanManage && !IsBusy);
        AddCommand = DatabaseCommand(AddAsync, () => CanManage && SelectedRoomType == null && RoomTypes.Count < 4);
        UpdateCommand = DatabaseCommand(UpdateAsync, () => CanManage && SelectedRoomType != null);
        DisableCommand = DatabaseCommand(DeleteAsync, () => CanManage && SelectedRoomType != null);
        SearchCommand = new RelayCommand(_ => ApplyFilters(), _ => !IsBusy);
        ResetFiltersCommand = new RelayCommand(_ => { SearchText = ""; MinimumCapacityText = ""; MaximumPriceText = ""; ApplyFilters(); }, _ => !IsBusy);
        ChooseImageCommand = new RelayCommand(_ => ChooseImage(), _ => CanManage && !IsBusy);
        ClearImageCommand = new RelayCommand(_ => ImagePath = null, _ => CanManage && !IsBusy && ImagePath != null);
        SelectedRoomType = null;
    }
    protected override async Task LoadDataAsync()
    {
        if (!CanView)
        {
            RoomTypes.Clear(); SelectedRoomType = null;
            throw new UnauthorizedAccessException("Only Staff and Manager can view room types.");
        }
        var data = await _service.GetAllRoomTypesAsync();
        RoomTypes.Clear(); foreach (var type in data) RoomTypes.Add(type);
        SelectedRoomType = CanManage ? null : RoomTypes.FirstOrDefault(); OnPropertyChanged(nameof(Summary));
    }
    private void ApplyFilters()
    {
        int? capacity = null; decimal? price = null;
        if (!string.IsNullOrWhiteSpace(MinimumCapacityText))
        {
            if (!int.TryParse(MinimumCapacityText, out int value) || value < 1)
            { Notify("Minimum capacity must be a positive integer.", "warning"); return; }
            capacity = value;
        }
        if (!string.IsNullOrWhiteSpace(MaximumPriceText))
        {
            if (!decimal.TryParse(MaximumPriceText, out decimal value) || value <= 0)
            { Notify("Maximum price must be a positive number.", "warning"); return; }
            price = value;
        }
        _minimumCapacity = capacity; _maximumPrice = price;
        RoomTypesView.Refresh();
    }
    private RoomTypeCreateDto FormData()
    {
        if (!int.TryParse(CapacityText, out int capacity)) throw new ArgumentException("Capacity must be a positive integer.");
        if (!decimal.TryParse(PriceText, out decimal price)) throw new ArgumentException("Enter a valid nightly price.");
        return new RoomTypeCreateDto { Name = TypeName, Capacity = capacity, BasePrice = price,
            Description = Description, RoomTypeImage = ImagePath };
    }
    private async Task AddAsync()
    {
        if (!CanManage) throw new UnauthorizedAccessException("Only Manager can add room types.");
        await _service.CreateRoomTypeAsync(FormData());
        await LoadDataAsync(); Notify("Room type added.", "success");
    }
    private async Task UpdateAsync()
    {
        if (!CanManage) throw new UnauthorizedAccessException("Only Manager can update room types.");
        if (SelectedRoomType == null) return;
        var form = FormData();
        await _service.UpdateRoomTypeAsync(new RoomTypeUpdateDto { RoomTypeId = SelectedRoomType.RoomTypeId,
            Name = form.Name, Capacity = form.Capacity, BasePrice = form.BasePrice,
            Description = form.Description, RoomTypeImage = form.RoomTypeImage });
        await LoadDataAsync(); Notify("Room type updated. Existing booking prices are preserved.", "success");
    }
    private async Task DeleteAsync()
    {
        if (!CanManage) throw new UnauthorizedAccessException("Only Manager can delete room types.");
        if (SelectedRoomType == null) return;
        if (MessageBox.Show($"Delete room type {SelectedRoomType.Name}?", "Delete room type", MessageBoxButton.YesNo,
            MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        await _service.DeleteRoomTypeAsync(SelectedRoomType.RoomTypeId);
        await LoadDataAsync(); Notify("Room type deleted.", "success");
    }
    private void ChooseImage()
    {
        if (!CanManage) return;
        var dialog = new OpenFileDialog { Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp" };
        if (dialog.ShowDialog() != true) return;
        try { ImagePath = RoomImageStorage.Import(dialog.FileName, "room-types"); }
        catch (Exception ex) { Notify(ex.Message, "error"); }
    }
}
