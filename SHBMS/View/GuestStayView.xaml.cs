using SHBMS.Helper;
using SHBMS.Model;
using System.Windows;
using System.Windows.Controls;

namespace SHBMS.View
{
    public partial class GuestStayView : UserControl
    {
        private const string AllStatuses = "All statuses";

        private GuestStay? _selected;   // row selected in the list
        private GuestStay? _arrival;    // booking being checked in (null = walk-in)
        private bool _ready;            // false while the screen is still being built
        private bool _suppress;         // true while the list is being rebuilt

        public GuestStayView()
        {
            InitializeComponent();

            StatusFilter.ItemsSource = new[] { AllStatuses, "Arriving", "In house", "Checked out" };
            StatusFilter.SelectedIndex = 0;

            GuestCombo.ItemsSource = HotelDemoStore.Customers;
            CheckOutPicker.DisplayDateStart = DateTime.Today.AddDays(1);

            _ready = true;

            SetWalkInMode();
            RefreshAll();
        }

        // ==========================================
        // REFRESH
        // ==========================================
        private void RefreshAll()
        {
            UpdateSummary();
            ApplyFilter();
            RefreshRoomChoices();
        }

        // Rooms may have been changed on the "Room status" screen meanwhile
        private void View_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_ready && e.NewValue is true)
                RefreshAll();
        }

        private void UpdateSummary()
        {
            var today = DateTime.Today;
            var stays = HotelDemoStore.Stays;

            InHouseCount.Text = stays.Count(s => s.Status == StayStatus.CheckedIn).ToString();

            ArrivingCount.Text = stays.Count(s =>
                s.Status == StayStatus.Confirmed && s.ArrivalDate.Date == today).ToString();

            DueOutCount.Text = stays.Count(s =>
                s.Status == StayStatus.CheckedIn && s.ExpectedCheckOut.Date <= today).ToString();
        }

        // ==========================================
        // FILTER
        // ==========================================
        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (!_ready)
                return;

            string keyword = SearchBox.Text.Trim();
            string status = StatusFilter.SelectedItem as string ?? AllStatuses;

            var stays = HotelDemoStore.Stays
                .Where(s =>
                    (status == AllStatuses || s.StatusText == status) &&
                    (keyword.Length == 0 ||
                     s.StayId.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                     s.BookingId.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                     s.GuestName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                     s.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(s => s.Status)                 // arriving, in house, checked out
                .ThenBy(s => s.ExpectedCheckOut)
                .ToList();

            // Rebuilding the list clears the selection, so remember it and put it back
            var previous = _selected;

            _suppress = true;
            StayGrid.ItemsSource = stays;
            if (previous != null && stays.Contains(previous))
                StayGrid.SelectedItem = previous;
            _suppress = false;

            _selected = StayGrid.SelectedItem as GuestStay;

            ResultText.Text = $"{stays.Count} of {HotelDemoStore.Stays.Count} stays";
            ShowDetail();
        }

        // ==========================================
        // SELECT STAY
        // ==========================================
        private void StayGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress)
                return;

            _selected = StayGrid.SelectedItem as GuestStay;
            ShowDetail();

            // Picking an arriving guest prepares the check-in form
            if (_selected?.Status == StayStatus.Confirmed)
                SetArrivalMode(_selected);
        }

        private void ShowDetail()
        {
            if (_selected is not GuestStay stay)
            {
                DetailPanel.Visibility = Visibility.Collapsed;
                EmptyDetail.Visibility = Visibility.Visible;
                return;
            }

            DetailPanel.Visibility = Visibility.Visible;
            EmptyDetail.Visibility = Visibility.Collapsed;

            DetailTitle.Text = $"{stay.StayId} · {stay.StatusText}";
            DetailGuest.Text = stay.GuestName;
            DetailRoom.Text = stay.RoomNumber.Length == 0
                ? $"Not assigned ({stay.RoomTypeName} booked)"
                : $"{stay.RoomNumber} · {stay.RoomTypeName}";
            DetailBooking.Text = stay.BookingRef;
            DetailCheckIn.Text = stay.CheckInTime.HasValue
                ? stay.CheckInTime.Value.ToString("dd/MM/yyyy HH:mm")
                : $"Arrives {stay.ArrivalDate:dd/MM/yyyy}";
            DetailCheckOut.Text = stay.ActualCheckOut.HasValue
                ? stay.ActualCheckOut.Value.ToString("dd/MM/yyyy HH:mm")
                : stay.ExpectedCheckOut.ToString("dd/MM/yyyy");

            var bill = stay.CalculateBill(DateTime.Now);

            BillCaption.Text = stay.Status switch
            {
                StayStatus.Confirmed => "ESTIMATED CHARGES",
                StayStatus.CheckedIn => "CHARGES IF CHECKED OUT NOW",
                _ => "FINAL CHARGES"
            };

            DetailNights.Text = $"{NightsText(bill.Nights)} × {stay.PricePerNight:N0} ₫";
            DetailRoomCharge.Text = Money(bill.RoomCharge);
            DetailServiceCharge.Text = Money(bill.ServiceCharge);
            DetailTotal.Text = Money(bill.Total);

            CheckOutButton.IsEnabled = stay.Status == StayStatus.CheckedIn;
        }

        // ==========================================
        // CHECK-IN FORM
        // ==========================================
        private void SetWalkInMode()
        {
            _arrival = null;

            CheckInModeText.Text = "Walk-in guest (no booking)";
            GuestCombo.IsEnabled = true;
            GuestCombo.SelectedItem = null;
            CheckOutPicker.SelectedDate = DateTime.Today.AddDays(1);

            RefreshRoomChoices();
        }

        private void SetArrivalMode(GuestStay booking)
        {
            _arrival = booking;

            CheckInModeText.Text =
                $"Booking {booking.BookingId} · {booking.RoomTypeName} room · arrives {booking.ArrivalDate:dd/MM/yyyy}";

            GuestCombo.SelectedItem = HotelDemoStore.Customers
                .FirstOrDefault(c => c.CustomerID == booking.CustomerId);
            GuestCombo.IsEnabled = false;   // the guest comes from the booking

            CheckOutPicker.SelectedDate = booking.ExpectedCheckOut.Date;

            RefreshRoomChoices();
        }

        // Only rooms that can really take a guest right now (Active and empty)
        private void RefreshRoomChoices()
        {
            if (!_ready)
                return;

            var previous = RoomCombo.SelectedItem as RoomInfo;

            var rooms = HotelDemoStore.GetAvailableRooms(_arrival?.RoomTypeName);
            RoomCombo.ItemsSource = rooms;

            if (previous != null && rooms.Contains(previous))
                RoomCombo.SelectedItem = previous;

            string typeText = _arrival != null ? $"{_arrival.RoomTypeName} " : "";
            RoomHint.Text = rooms.Count == 0
                ? $"No {typeText}room is available right now."
                : $"{rooms.Count} {typeText}room(s) available.";
        }

        private void ResetCheckIn_Click(object sender, RoutedEventArgs e)
        {
            SetWalkInMode();
        }

        // ==========================================
        // [UC-07] CHECK IN
        // ==========================================
        private void CheckIn_Click(object sender, RoutedEventArgs e)
        {
            if (GuestCombo.SelectedItem is not CustomerProfile customer)
            {
                ToastService.Show("Please select a guest.", "warning");
                return;
            }

            if (RoomCombo.SelectedItem is not RoomInfo room)
            {
                ToastService.Show("Please select an available room.", "warning");
                return;
            }

            if (CheckOutPicker.SelectedDate is not DateTime checkOut)
            {
                ToastService.Show("Please choose the expected check-out date.", "warning");
                return;
            }

            if (checkOut.Date <= DateTime.Today)
            {
                ToastService.Show("Check-out date must be after today.", "warning");
                return;
            }

            if (_arrival != null && _arrival.ArrivalDate.Date > DateTime.Today)
            {
                ToastService.Show(
                    $"This booking arrives on {_arrival.ArrivalDate:dd/MM/yyyy}. It is too early to check in.",
                    "warning");
                return;
            }

            if (!room.IsAvailable)
            {
                ToastService.Show($"Room {room.RoomNumber} is no longer available.", "error");
                RefreshRoomChoices();
                return;
            }

            bool alreadyInHouse = HotelDemoStore.Stays.Any(s =>
                s.CustomerId == customer.CustomerID &&
                s.Status == StayStatus.CheckedIn);

            if (alreadyInHouse)
            {
                ToastService.Show($"{customer.FullName} is already checked in.", "warning");
                return;
            }

            GuestStay stay;

            if (_arrival != null)
            {
                stay = _arrival;   // booking already exists
            }
            else
            {
                stay = new GuestStay
                {
                    StayId = HotelDemoStore.NextStayId(),
                    BookingId = "",   // walk-in
                    CustomerId = customer.CustomerID,
                    GuestName = customer.FullName,
                    RoomTypeName = room.RoomTypeName,
                    PricePerNight = room.PricePerNight,
                    ArrivalDate = DateTime.Today
                };

                HotelDemoStore.Stays.Add(stay);
            }

            HotelDemoStore.CheckIn(stay, room, checkOut);

            _selected = stay;
            SetWalkInMode();
            RefreshAll();

            ToastService.Show(
                $"{stay.GuestName} checked in to room {room.RoomNumber}.",
                "success");
        }

        // ==========================================
        // [UC-08] CHECK OUT
        // ==========================================
        private void CheckOut_Click(object sender, RoutedEventArgs e)
        {
            if (_selected is not GuestStay stay || stay.Status != StayStatus.CheckedIn)
            {
                ToastService.Show("Select a guest who is currently in house.", "warning");
                return;
            }

            var bill = stay.CalculateBill(DateTime.Now);

            var confirm = MessageBox.Show(
                $"Check out {stay.GuestName} from room {stay.RoomNumber}?\n\n" +
                $"{NightsText(bill.Nights)} × {stay.PricePerNight:N0} ₫ = {Money(bill.RoomCharge)}\n" +
                $"Services: {Money(bill.ServiceCharge)}\n\n" +
                $"Total due: {Money(bill.Total)}",
                "Confirm check-out",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            HotelDemoStore.CheckOut(stay);

            RefreshAll();

            ToastService.Show(
                $"{stay.GuestName} checked out. Room {stay.RoomNumber} is free again.",
                "success");
        }

        // ==========================================
        // HELPERS
        // ==========================================
        private static string Money(decimal amount) => $"{amount:N0} ₫";

        private static string NightsText(int nights) => nights == 1 ? "1 night" : $"{nights} nights";
    }
}
