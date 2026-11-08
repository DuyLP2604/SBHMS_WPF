using Microsoft.EntityFrameworkCore;
using SHBMS.Model;
using SHBMS.DTO.Booking;
using SHBMS.Helper;
using SHBMS.Service.Interface;
using System.Collections.ObjectModel;
using System.Windows.Input;
namespace SHBMS.ViewModel;
public class CreateBookingViewModel : DatabaseViewModel
{
    private readonly IBookingService _service;
    public bool CanCreate => Session.GetRole() == "Staff";
    public ObservableCollection<BookingCustomerDto> CustomerMatches { get; } = new();
    public ObservableCollection<string> Nationalities { get; } = new();
    public ObservableCollection<AvailableRoomTypeDto> AvailableTypes { get; } = new();
    public ObservableCollection<BookingRoomLineDto> Cart { get; } = new();
    public DateTime Today => DateTime.Today;
    public int NumberOfNights => CheckInDate.HasValue && CheckOutDate.HasValue
        ? Math.Max(0, (CheckOutDate.Value.Date - CheckInDate.Value.Date).Days) : 0;
    public decimal TotalAmount => Cart.Sum(r => r.LineTotal);
    public int TotalRooms => Cart.Sum(r => r.Quantity);
    private string _phoneSearch = "";
    public string PhoneSearch { get => _phoneSearch; set => SetProperty(ref _phoneSearch, value); }
    private string _fullName = "";
    public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
    private string _phone = "";
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    private string _identityNumber = "";
    public string IdentityNumber { get => _identityNumber; set => SetProperty(ref _identityNumber, value); }
    private string _email = "";
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    private string _address = "";
    public string Address { get => _address; set => SetProperty(ref _address, value); }
    private string _nationalityName = "";
    public string NationalityName { get => _nationalityName; set => SetProperty(ref _nationalityName, value ?? ""); }
    private string _quantityText = "";
    public string QuantityText { get => _quantityText; set => SetProperty(ref _quantityText, value); }
    private bool _isNewCustomer;
    public bool IsNewCustomer
    {
        get => _isNewCustomer;
        private set { SetProperty(ref _isNewCustomer, value); OnPropertyChanged(nameof(IsCustomerReadOnly)); OnPropertyChanged(nameof(CustomerModeText)); }
    }
    public bool IsCustomerReadOnly => !IsNewCustomer;
    public string CustomerModeText => IsNewCustomer ? "New customer - saved together with the booking."
        : SelectedCustomer == null ? "Search and select an existing customer, or choose New customer." : "Selected customer: " + SelectedCustomer.CustomerId;
    private BookingCustomerDto? _selectedCustomer;
    public BookingCustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            SetProperty(ref _selectedCustomer, value);
            if (value != null) IsNewCustomer = false;
            FullName = value?.FullName ?? ""; Phone = value?.Phone ?? "";
            IdentityNumber = value?.IdentityNumber ?? ""; Email = value?.Email ?? "";
            Address = value?.Address ?? ""; NationalityName = value?.NationalityName ?? "";
            OnPropertyChanged(nameof(CustomerModeText)); CommandManager.InvalidateRequerySuggested();
        }
    }
    private DateTime? _checkInDate = DateTime.Today;
    public DateTime? CheckInDate
    {
        get => _checkInDate;
        set { if (SetProperty(ref _checkInDate, value)) { InvalidateRooms(); OnPropertyChanged(nameof(NumberOfNights)); } }
    }
    private DateTime? _checkOutDate = DateTime.Today.AddDays(1);
    public DateTime? CheckOutDate
    {
        get => _checkOutDate;
        set { if (SetProperty(ref _checkOutDate, value)) { InvalidateRooms(); OnPropertyChanged(nameof(NumberOfNights)); } }
    }
    private bool _hasRoomSearch;
    public bool HasRoomSearch { get => _hasRoomSearch; private set => SetProperty(ref _hasRoomSearch, value); }
    private AvailableRoomTypeDto? _selectedAvailableType;
    public AvailableRoomTypeDto? SelectedAvailableType { get => _selectedAvailableType; set => SetProperty(ref _selectedAvailableType, value); }
    private BookingRoomLineDto? _selectedCartItem;
    public BookingRoomLineDto? SelectedCartItem { get => _selectedCartItem; set => SetProperty(ref _selectedCartItem, value); }
    public RelayCommand SearchCustomersCommand { get; }
    public RelayCommand NewCustomerCommand { get; }
    public RelayCommand SearchRoomsCommand { get; }
    public RelayCommand AddRoomCommand { get; }
    public RelayCommand RemoveRoomCommand { get; }
    public RelayCommand ConfirmCommand { get; }
    public RelayCommand BackCommand { get; }
    public event Action? BackRequested;
    public event Action<string>? BookingCreated;
    public CreateBookingViewModel(IBookingService? service = null)
    {
        _service = service ?? new SHBMS.Service.Implementation.BookingService();
        QuantityText = "1";
        SearchCustomersCommand = DatabaseCommand(SearchCustomersAsync, () => CanCreate);
        NewCustomerCommand = new RelayCommand(_ => { SelectedCustomer = null; IsNewCustomer = true; Phone = PhoneSearch.Trim(); }, _ => CanCreate && !IsBusy);
        SearchRoomsCommand = DatabaseCommand(SearchRoomsAsync, () => CanCreate);
        AddRoomCommand = new RelayCommand(_ => AddRoom(), _ => CanCreate && !IsBusy && HasRoomSearch && SelectedAvailableType?.AvailableRooms > 0);
        RemoveRoomCommand = new RelayCommand(_ => { if (SelectedCartItem != null) Cart.Remove(SelectedCartItem); }, _ => CanCreate && !IsBusy && SelectedCartItem != null);
        ConfirmCommand = DatabaseCommand(ConfirmAsync,
            () => CanCreate && HasRoomSearch && Cart.Count > 0 && (IsNewCustomer || SelectedCustomer != null));
        BackCommand = new RelayCommand(_ => BackRequested?.Invoke(), _ => !IsBusy);
        Cart.CollectionChanged += (_, _) => { OnPropertyChanged(nameof(TotalAmount)); OnPropertyChanged(nameof(TotalRooms)); CommandManager.InvalidateRequerySuggested(); };
    }
    protected override async Task LoadDataAsync()
    {
        if (!CanCreate)
        {
            CustomerMatches.Clear(); SelectedCustomer = null; InvalidateRooms();
            throw new UnauthorizedAccessException("Only Staff can create bookings.");
        }
        using var db = new Prn212SbhmsContext();
        var nations = await db.Nationalities.AsNoTracking().OrderBy(n => n.NationalityName)
            .Select(n => n.NationalityName).ToListAsync();
        string selected = NationalityName;
        Nationalities.Clear(); Nationalities.Add("");
        foreach (var nation in nations) Nationalities.Add(nation);
        NationalityName = selected;
    }
    private async Task SearchCustomersAsync()
    {
        var matches = await _service.SearchCustomersAsync(PhoneSearch);
        IsNewCustomer = false; SelectedCustomer = null;
        CustomerMatches.Clear(); foreach (var c in matches) CustomerMatches.Add(c);
        if (matches.Count == 1) SelectedCustomer = matches[0];
        StatusMessage = matches.Count == 0 ? "No matching customers. Choose New customer to enter their details."
            : matches.Count > 1 ? "Multiple customers found. Select the correct row." : "Customer selected.";
    }
    private (DateOnly Arrival, DateOnly Departure) Dates()
    {
        if (!CheckInDate.HasValue || !CheckOutDate.HasValue) throw new ArgumentException("Choose check-in and check-out dates.");
        return (DateOnly.FromDateTime(CheckInDate.Value), DateOnly.FromDateTime(CheckOutDate.Value));
    }
    private void InvalidateRooms()
    {
        StatusMessage = ""; HasRoomSearch = false; SelectedAvailableType = null; SelectedCartItem = null;
        AvailableTypes.Clear(); Cart.Clear(); CommandManager.InvalidateRequerySuggested();
    }
    private async Task SearchRoomsAsync()
    {
        var dates = Dates();
        InvalidateRooms();
        var types = await _service.GetAvailableRoomTypesAsync(dates.Arrival, dates.Departure);
        foreach (var type in types) AvailableTypes.Add(type);
        HasRoomSearch = true;
        StatusMessage = types.Any(t => t.AvailableRooms > 0) ? "Select a room type, enter quantity and add it to the booking." : "No rooms are available for these dates.";
    }
    private void AddRoom()
    {
        if (!CanCreate || !HasRoomSearch || SelectedAvailableType == null) return;
        if (!int.TryParse(QuantityText, out int quantity) || quantity <= 0)
        { 
            StatusMessage = "Quantity must be a positive integer."; return; 
        }
        var type = SelectedAvailableType;
        var existing = Cart.FirstOrDefault(r => r.RoomTypeId == type.RoomTypeId);
        long combined = (long)(existing?.Quantity ?? 0) + quantity;
        if (combined > type.AvailableRooms) { StatusMessage = "The requested quantity exceeds available rooms."; return; }
        if (existing != null) Cart.Remove(existing);
        Cart.Add(new BookingRoomLineDto { RoomTypeId = type.RoomTypeId, TypeName = type.Name,
            Quantity = (int)combined, UnitPrice = type.UnitPrice, NumberOfNights = NumberOfNights });
        StatusMessage = "Room selection updated. To change a quantity, remove its row and add it again.";
    }
    private async Task ConfirmAsync()
    {
        if (!CanCreate) throw new UnauthorizedAccessException("Only Staff can create bookings.");
        var dates = Dates();
        var request = new BookingCreateDto {
            CustomerId = IsNewCustomer ? null : SelectedCustomer?.CustomerId,
            NewCustomer = IsNewCustomer ? new NewBookingCustomerDto { FullName = FullName, Phone = Phone,
                IdentityNumber = IdentityNumber, Email = Email, Address = Address, NationalityName = NationalityName } : null,
            CheckInDate = dates.Arrival, CheckOutDate = dates.Departure,
            Rooms = Cart.Select(r => new BookingRoomRequestDto { RoomTypeId = r.RoomTypeId, Quantity = r.Quantity, QuotedUnitPrice = r.UnitPrice }).ToList()
        };
        string id;
        try { id = await _service.CreateBookingAsync(request); }
        catch (InvalidOperationException) { InvalidateRooms(); throw; }
        BookingCreated?.Invoke(id);
    }
}
