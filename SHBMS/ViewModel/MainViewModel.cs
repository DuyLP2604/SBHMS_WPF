using SHBMS.Helper;

namespace SHBMS.ViewModel;

public sealed class MainViewModel : BaseViewModel
{
    private object? _currentPage;
    private string _pageKey = "Customers";
    private string _role = "Staff";
    public object? CurrentPage { get => _currentPage; private set => SetProperty(ref _currentPage, value); }
    public string CurrentPageKey
    {
        get => _pageKey;
        private set
        {
            if (!SetProperty(ref _pageKey, value)) return;
            OnPropertyChanged(nameof(IsOperationsPage));
            OnPropertyChanged(nameof(PageTitle));
        }
    }
    public bool IsOperationsPage => CurrentPageKey is "Bookings" or "Rooms" or "Room types";
    public string PageTitle => IsOperationsPage ? $"Hotel operations / {CurrentPageKey}" : "Hotel operations / Guest relations";
    public string[] Roles { get; } = new[] { "Staff", "Manager" };
    public bool IsStaff => SelectedRole == "Staff";
    public bool IsManager => SelectedRole == "Manager";
    public bool CanViewRooms => IsStaff;
    public string SelectedRole
    {
        get => _role;
        set
        {
            if (!SetProperty(ref _role, value)) return;
            OnPropertyChanged(nameof(CanViewRooms));
            Bookings.RefreshRole(); RoomTypes.RefreshRole();
            if (!IsStaff && CurrentPageKey == "Rooms") Navigate(Bookings, "Bookings");
        }
    }
    public BookingsViewModel Bookings { get; }
    public RoomsViewModel Rooms { get; }
    public RoomTypesViewModel RoomTypes { get; }
    public RelayCommand BookingsCommand { get; }
    public RelayCommand RoomsCommand { get; }
    public RelayCommand RoomTypesCommand { get; }

    public MainViewModel()
    {
        Bookings = new BookingsViewModel(() => IsStaff);
        Rooms = new RoomsViewModel();
        RoomTypes = new RoomTypesViewModel(() => IsManager);
        BookingsCommand = Command(() => Navigate(Bookings, "Bookings"));
        RoomsCommand = Command(() => Navigate(Rooms, "Rooms"), () => IsStaff);
        RoomTypesCommand = Command(() => Navigate(RoomTypes, "Room types"));
    }

    public void ShowLegacyPage(string key)
    {
        CurrentPage = null;
        CurrentPageKey = key;
    }

    private void Navigate(object page, string key)
    {
        CurrentPage = page;
        CurrentPageKey = key;
    }
}
