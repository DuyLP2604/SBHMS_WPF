using SBHMS.DTO.Room;
using SHBMS.Helper;
using SHBMS.Model;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SHBMS.View
{
    public partial class RoomStatusView : UserControl
    {
        private const string AllStatuses = "All statuses";
        private const string AllTypes = "All room types";

        private RoomInfo? _selected;
        private bool _ready;      // false while the screen is still being built
        private bool _suppress;   // true while the list is being rebuilt

        public RoomStatusView()
        {
            InitializeComponent();

            StatusFilter.ItemsSource = new[]
            {
                AllStatuses,
                RoomInfo.StatusAvailable,
                RoomInfo.StatusOccupied,
                RoomInfo.StatusMaintenance,
                RoomInfo.StatusInactive
            };
            StatusFilter.SelectedIndex = 0;

            TypeFilter.ItemsSource = new[] { AllTypes }
                .Concat(HotelDemoStore.Rooms.Select(r => r.RoomTypeName).Distinct())
                .ToList();
            TypeFilter.SelectedIndex = 0;

            // Choices for [UC-14] Update Room Status: Active / Inactive / Maintenance
            NewStatusCombo.ItemsSource = Enum.GetValues<RoomStatusEnum>();

            // Rooms change when guests check in/out on the other screen
            foreach (var room in HotelDemoStore.Rooms)
                room.PropertyChanged += Room_PropertyChanged;

            _ready = true;
            RefreshBoard();
        }

        // ==========================================
        // REFRESH
        // ==========================================
        private void RefreshBoard()
        {
            UpdateSummary();
            ApplyFilter();
            LastRefreshText.Text = $"Last refreshed {DateTime.Now:HH:mm:ss}";
        }

        private void Room_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RoomInfo.DisplayStatus))
                RefreshBoard();
        }

        private void View_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_ready && e.NewValue is true)
                RefreshBoard();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshBoard();
            ToastService.Show("Room board refreshed.", "success");
        }

        // ==========================================
        // SUMMARY
        // ==========================================
        private void UpdateSummary()
        {
            var rooms = HotelDemoStore.Rooms;

            AvailableCount.Text = rooms.Count(r => r.DisplayStatus == RoomInfo.StatusAvailable).ToString();
            OccupiedCount.Text = rooms.Count(r => r.DisplayStatus == RoomInfo.StatusOccupied).ToString();
            MaintenanceCount.Text = rooms.Count(r => r.DisplayStatus == RoomInfo.StatusMaintenance).ToString();
            InactiveCount.Text = rooms.Count(r => r.DisplayStatus == RoomInfo.StatusInactive).ToString();
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
            string type = TypeFilter.SelectedItem as string ?? AllTypes;

            var rooms = HotelDemoStore.Rooms
                .Where(r =>
                    (status == AllStatuses || r.DisplayStatus == status) &&
                    (type == AllTypes || r.RoomTypeName == type) &&
                    (keyword.Length == 0 ||
                     r.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                     r.CurrentGuestName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(r => r.RoomNumber)
                .ToList();

            // Rebuilding the list clears the selection, so remember it and put it back
            var previous = _selected;

            _suppress = true;
            RoomList.ItemsSource = rooms;
            if (previous != null && rooms.Contains(previous))
                RoomList.SelectedItem = previous;
            _suppress = false;

            _selected = RoomList.SelectedItem as RoomInfo;

            ResultText.Text = $"{rooms.Count} of {HotelDemoStore.Rooms.Count} rooms";
            ShowDetail();
        }

        // ==========================================
        // SELECT ROOM
        // ==========================================
        private void RoomList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress)
                return;

            _selected = RoomList.SelectedItem as RoomInfo;
            ShowDetail();
        }

        private void ShowDetail()
        {
            if (_selected is not RoomInfo room)
            {
                DetailPanel.Visibility = Visibility.Collapsed;
                EmptyDetail.Visibility = Visibility.Visible;
                return;
            }

            DetailPanel.Visibility = Visibility.Visible;
            EmptyDetail.Visibility = Visibility.Collapsed;

            DetailRoomNumber.Text = room.RoomNumber;
            DetailTypeText.Text = $"{room.RoomTypeName} · up to {room.Capacity} guests";
            DetailRate.Text = $"{room.PricePerNight:N0} ₫ / night";

            DetailGuest.Text = room.IsOccupied ? room.CurrentGuestName : "—";
            DetailCheckOut.Text = room.IsOccupied && room.ExpectedCheckOut.HasValue
                ? room.ExpectedCheckOut.Value.ToString("dd/MM/yyyy")
                : "—";

            // Status pill
            var (background, foreground) = Palette(room.DisplayStatus);
            DetailStatusPill.Background = background;
            DetailStatusText.Foreground = foreground;
            DetailStatusText.Text = room.DisplayStatus;

            // Update area
            NewStatusCombo.SelectedItem = room.Status;

            if (room.IsOccupied)
            {
                NewStatusCombo.IsEnabled = false;
                UpdateStatusButton.IsEnabled = false;
                StatusHint.Text = "A guest is staying in this room. Its status can be changed after check-out.";
            }
            else
            {
                NewStatusCombo.IsEnabled = true;
                UpdateStatusButton.IsEnabled = true;
                StatusHint.Text = "Active = open for bookings. Maintenance and Inactive take the room out of service.";
            }
        }

        // ==========================================
        // [UC-14] UPDATE ROOM STATUS
        // ==========================================
        private void UpdateStatus_Click(object sender, RoutedEventArgs e)
        {
            if (_selected is not RoomInfo room)
            {
                ToastService.Show("Please select a room first.", "warning");
                return;
            }

            if (NewStatusCombo.SelectedItem is not RoomStatusEnum newStatus)
            {
                ToastService.Show("Please choose a new status.", "warning");
                return;
            }

            if (room.IsOccupied)
            {
                ToastService.Show(
                    $"Room {room.RoomNumber} is occupied by {room.CurrentGuestName}. Check the guest out first.",
                    "warning");
                return;
            }

            if (room.Status == newStatus)
            {
                ToastService.Show(
                    $"Room {room.RoomNumber} is already {newStatus}.",
                    "warning");
                return;
            }

            room.Status = newStatus;   // the board, counters and details refresh automatically

            ToastService.Show(
                $"Room {room.RoomNumber} is now {newStatus}.",
                "success");
        }

        // ==========================================
        // COLOURS
        // ==========================================
        private static (Brush Background, Brush Foreground) Palette(string displayStatus)
        {
            return displayStatus switch
            {
                RoomInfo.StatusOccupied => (BrushFrom("#E8EFFF"), BrushFrom("#2F54C8")),
                RoomInfo.StatusMaintenance => (BrushFrom("#FFF4DD"), BrushFrom("#B7791F")),
                RoomInfo.StatusInactive => (BrushFrom("#EEF0F4"), BrushFrom("#6B7686")),
                _ => (BrushFrom("#EAF7F0"), BrushFrom("#167A4B"))
            };
        }

        private static Brush BrushFrom(string hex)
            => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }
}
