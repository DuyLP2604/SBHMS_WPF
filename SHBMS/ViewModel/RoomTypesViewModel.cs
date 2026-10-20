using System.Collections.ObjectModel;
using SHBMS.Helper;
using SHBMS.Model;

namespace SHBMS.ViewModel;

public sealed class RoomTypesViewModel : OperationViewModel
{
    private readonly Func<bool> _isManager;
    private readonly IReadOnlyList<UiRoomType> _samples = UiSampleData.RoomTypes();
    private UiRoomType? _selected;
    private string _search = "";
    private string _name = "";
    private string _capacity = "1";
    private string _price = "";
    private string _description = "";
    public ObservableCollection<UiRoomType> RoomTypes { get; } = new();
    public string SearchText { get => _search; set { if (SetProperty(ref _search, value)) Filter(); } }
    public UiRoomType? SelectedRoomType
    {
        get => _selected;
        set
        {
            if (!SetProperty(ref _selected, value)) return;
            Name = value?.Name ?? ""; CapacityText = value?.Capacity.ToString() ?? "1";
            PriceText = value?.BasePrice.ToString("0.##") ?? ""; Description = value?.Description ?? "";
            OnPropertyChanged(nameof(FormTitle)); OnPropertyChanged(nameof(SelectedTypeDescription));
        }
    }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string CapacityText { get => _capacity; set => SetProperty(ref _capacity, value); }
    public string PriceText { get => _price; set => SetProperty(ref _price, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public bool CanManage => _isManager();
    public string FormTitle => SelectedRoomType == null ? "New room type" : $"Edit {SelectedRoomType.RoomTypeId}";
    public string SelectedTypeDescription => SelectedRoomType?.Description ?? "Select a room type to read its description.";
    public int TypeCount => RoomTypes.Count;
    public RelayCommand RefreshCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public RoomTypesViewModel(Func<bool> isManager)
    {
        _isManager = isManager;
        RefreshCommand = Command(Filter);
        NewCommand = Command(NewForm, () => CanManage);
        SaveCommand = Command(() => Message = "Interface preview only. Saving a room type is not connected yet.", () => CanManage);
        DeleteCommand = Command(() => Message = "Interface preview only. Deleting a room type is not connected yet.", () => CanManage && SelectedRoomType?.CanDelete == true);
        Filter();
    }

    public void RefreshRole() => OnPropertyChanged(nameof(CanManage));

    private void NewForm()
    {
        SelectedRoomType = null; Name = ""; CapacityText = "1"; PriceText = ""; Description = "";
        OnPropertyChanged(nameof(FormTitle));
    }

    private void Filter()
    {
        var id = SelectedRoomType?.RoomTypeId;
        RoomTypes.Clear();
        foreach (var type in _samples.Where(x => string.IsNullOrWhiteSpace(SearchText) ||
            x.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase) ||
            x.RoomTypeId.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase))) RoomTypes.Add(type);
        SelectedRoomType = RoomTypes.FirstOrDefault(x => x.RoomTypeId == id);
        OnPropertyChanged(nameof(TypeCount));
    }
}
