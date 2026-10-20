using System.Collections.ObjectModel;
using SHBMS.Helper;
using SHBMS.Model;

namespace SHBMS.ViewModel;

public sealed class BookingsViewModel : OperationViewModel
{
    private readonly Func<bool> _isStaff;
    private readonly IReadOnlyList<UiBooking> _samples = UiSampleData.Bookings();
    private UiBooking? _selected;
    private UiBookingDetail? _detail;
    private string _search = "";
    private string _status = "All";
    private bool _creating;
    public ObservableCollection<UiBooking> Bookings { get; } = new();
    public string[] Statuses { get; } = new[] { "All", "PENDING", "CONFIRMED", "CHECKED_IN", "CHECKED_OUT", "CANCELLED" };
    public string SearchText { get => _search; set { if (SetProperty(ref _search, value)) Filter(); } }
    public string SelectedStatus { get => _status; set { if (SetProperty(ref _status, value)) Filter(); } }
    public UiBooking? SelectedBooking { get => _selected; set { if (SetProperty(ref _selected, value)) SelectedDetail = null; } }
    public UiBookingDetail? SelectedDetail { get => _detail; private set => SetProperty(ref _detail, value); }
    public bool IsCreating { get => _creating; private set => SetProperty(ref _creating, value); }
    public bool CanCreate => _isStaff();
    public int BookingCount => Bookings.Count;
    public BookingEditorViewModel Editor { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand ViewDetailCommand { get; }
    public RelayCommand NewBookingCommand { get; }
    public RelayCommand DeleteExpiredCommand { get; }
    public RelayCommand CloseDetailCommand { get; }

    public BookingsViewModel(Func<bool> isStaff)
    {
        _isStaff = isStaff;
        Editor = new BookingEditorViewModel(() => IsCreating = false);
        RefreshCommand = Command(() => { Filter(); Message = "Sample bookings displayed."; });
        ViewDetailCommand = Command(() => SelectedDetail = UiSampleData.Detail(SelectedBooking!), () => SelectedBooking != null);
        NewBookingCommand = Command(() => { Editor.Reset(); IsCreating = true; }, () => CanCreate);
        DeleteExpiredCommand = Command(() => Message = "Interface preview only. Booking deletion is not connected yet.", () => CanCreate && SelectedBooking?.IsOverdueCheckIn == true);
        CloseDetailCommand = Command(() => SelectedDetail = null);
        Filter();
    }

    public void RefreshRole()
    {
        IsCreating = false;
        OnPropertyChanged(nameof(CanCreate));
    }

    private void Filter()
    {
        var id = SelectedBooking?.BookingId;
        Bookings.Clear();
        foreach (var booking in _samples.Where(x =>
            (SelectedStatus == "All" || x.Status == SelectedStatus) &&
            (string.IsNullOrWhiteSpace(SearchText) || new[] { x.BookingId, x.CustomerName, x.CustomerPhone }
                .Any(text => text.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase))))) Bookings.Add(booking);
        SelectedBooking = Bookings.FirstOrDefault(x => x.BookingId == id);
        OnPropertyChanged(nameof(BookingCount));
    }
}
