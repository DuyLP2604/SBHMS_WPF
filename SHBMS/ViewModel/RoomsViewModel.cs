using System.Collections.ObjectModel;
using SHBMS.Helper;
using SHBMS.Model;

namespace SHBMS.ViewModel;

public sealed class RoomsViewModel : OperationViewModel
{
    private string _search = "";
    private string _status = "All";
    public ObservableCollection<UiRoom> Rooms { get; } = new();
    public string[] Statuses { get; } = new[] { "All", "ACTIVE", "MAINTENANCE", "INACTIVE" };
    public string SearchText { get => _search; set { if (SetProperty(ref _search, value)) Filter(); } }
    public string SelectedStatus { get => _status; set { if (SetProperty(ref _status, value)) Filter(); } }
    public int RoomCount => Rooms.Count;
    public RelayCommand RefreshCommand { get; }

    public RoomsViewModel()
    {
        RefreshCommand = Command(Filter);
        Filter();
    }

    private void Filter()
    {
        Rooms.Clear();
        foreach (var room in UiSampleData.Rooms().Where(x =>
            (SelectedStatus == "All" || x.Status == SelectedStatus) &&
            (string.IsNullOrWhiteSpace(SearchText) || x.RoomNumber.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)
                || x.RoomTypeName.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)))) Rooms.Add(room);
        OnPropertyChanged(nameof(RoomCount));
    }
}
