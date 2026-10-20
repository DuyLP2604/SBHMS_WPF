using System.Collections.ObjectModel;
using SHBMS.Helper;
using SHBMS.Model;

namespace SHBMS.ViewModel;

public sealed class BookingEditorViewModel : OperationViewModel
{
    private UiCustomer? _customer;
    private UiRoomType? _roomType;
    private UiRoomLine? _cartLine;
    private DateTime? _checkIn = DateTime.Today.AddDays(1);
    private DateTime? _checkOut = DateTime.Today.AddDays(2);
    private string _quantity = "1";
    public ObservableCollection<UiCustomer> Customers { get; } = new(UiSampleData.Customers());
    public ObservableCollection<UiRoomType> AvailableTypes { get; } = new(UiSampleData.RoomTypes().Where(x => x.AvailableRooms > 0));
    public ObservableCollection<UiRoomLine> Cart { get; } = new();
    public UiCustomer? SelectedCustomer { get => _customer; set => SetProperty(ref _customer, value); }
    public UiRoomType? SelectedRoomType { get => _roomType; set => SetProperty(ref _roomType, value); }
    public UiRoomLine? SelectedCartLine { get => _cartLine; set => SetProperty(ref _cartLine, value); }
    public string QuantityText { get => _quantity; set => SetProperty(ref _quantity, value); }
    public DateTime? CheckInDate { get => _checkIn; set { if (SetProperty(ref _checkIn, value)) DatesChanged(); } }
    public DateTime? CheckOutDate { get => _checkOut; set { if (SetProperty(ref _checkOut, value)) DatesChanged(); } }
    public int NumberOfNights => CheckInDate.HasValue && CheckOutDate.HasValue ? Math.Max(0, (CheckOutDate.Value.Date - CheckInDate.Value.Date).Days) : 0;
    public DateTime? CheckInDeadline => CheckInDate?.Date.AddHours(14).AddMinutes(15);
    public decimal EstimatedTotal => Cart.Sum(x => x.LineTotal);
    public int TotalRooms => Cart.Sum(x => x.Quantity);
    public RelayCommand CheckAvailabilityCommand { get; }
    public RelayCommand AddRoomsCommand { get; }
    public RelayCommand RemoveRoomsCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public BookingEditorViewModel(Action cancel)
    {
        CheckAvailabilityCommand = Command(() => Message = "These are sample room types for the interface preview.");
        AddRoomsCommand = Command(AddRooms, () => SelectedRoomType != null && NumberOfNights > 0 && int.TryParse(QuantityText, out var quantity) && quantity > 0);
        RemoveRoomsCommand = Command(() => { Cart.Remove(SelectedCartLine!); SelectedCartLine = null; UpdateTotals(); }, () => SelectedCartLine != null);
        SaveCommand = Command(() => Message = "Interface preview only. Creating a booking is not connected yet.", () => SelectedCustomer != null && Cart.Count > 0);
        CancelCommand = Command(cancel);
    }

    public void Reset()
    {
        SelectedCustomer = null; SelectedRoomType = null; SelectedCartLine = null;
        QuantityText = "1"; Cart.Clear(); Message = ""; UpdateTotals();
    }

    private void AddRooms()
    {
        var type = SelectedRoomType!;
        var old = Cart.FirstOrDefault(x => x.RoomTypeId == type.RoomTypeId);
        if (old != null) Cart.Remove(old);
        var line = new UiRoomLine(type.RoomTypeId, type.Name, type.Capacity, int.Parse(QuantityText), type.BasePrice, NumberOfNights);
        Cart.Add(line); SelectedCartLine = line; UpdateTotals();
    }

    private void DatesChanged()
    {
        Cart.Clear(); SelectedCartLine = null;
        OnPropertyChanged(nameof(NumberOfNights)); OnPropertyChanged(nameof(CheckInDeadline)); UpdateTotals();
    }

    private void UpdateTotals()
    {
        OnPropertyChanged(nameof(EstimatedTotal)); OnPropertyChanged(nameof(TotalRooms));
    }
}
