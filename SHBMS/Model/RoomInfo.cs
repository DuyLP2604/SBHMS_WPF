using SBHMS.DTO.Room;

namespace SHBMS.Model
{
    /// <summary>
    /// One physical room as shown on the Room status board.
    /// Mirrors ROOM + ROOM_TYPE in the database.
    /// "Occupied" is NOT stored: it is derived from whether a guest is in the room
    /// (same idea as RoomStatusDto.CurrentGuestName).
    /// </summary>
    public class RoomInfo : NotifyObject
    {
        // Values shown on screen (also used by the XAML colour triggers)
        public const string StatusAvailable = "Available";
        public const string StatusOccupied = "Occupied";
        public const string StatusMaintenance = "Maintenance";
        public const string StatusInactive = "Inactive";

        public string RoomId { get; set; } = "";          // CHAR(3)
        public string RoomNumber { get; set; } = "";
        public string RoomTypeId { get; set; } = "";      // CHAR(4)
        public string RoomTypeName { get; set; } = "";
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }

        private RoomStatusEnum _status = RoomStatusEnum.Active;
        public RoomStatusEnum Status
        {
            get => _status;
            set { if (Set(ref _status, value)) RaiseDerived(); }
        }

        private string _currentGuestName = "";
        public string CurrentGuestName
        {
            get => _currentGuestName;
            set { if (Set(ref _currentGuestName, value)) RaiseDerived(); }
        }

        private DateTime? _expectedCheckOut;
        public DateTime? ExpectedCheckOut
        {
            get => _expectedCheckOut;
            set => Set(ref _expectedCheckOut, value);
        }

        public bool IsOccupied => _currentGuestName.Length > 0;

        /// <summary>Can a new guest be put in this room right now?</summary>
        public bool IsAvailable => Status == RoomStatusEnum.Active && !IsOccupied;

        public string DisplayStatus => Status switch
        {
            RoomStatusEnum.Maintenance => StatusMaintenance,
            RoomStatusEnum.Inactive => StatusInactive,
            _ => IsOccupied ? StatusOccupied : StatusAvailable
        };

        /// <summary>Short line at the bottom of the room tile.</summary>
        public string TileNote => DisplayStatus switch
        {
            StatusOccupied => CurrentGuestName,
            StatusMaintenance => "Under maintenance",
            StatusInactive => "Out of service",
            _ => "Ready for check-in"
        };

        /// <summary>Text used in the room drop-down when checking in.</summary>
        public string Label => $"{RoomNumber} · {RoomTypeName} · {PricePerNight:N0} ₫";

        private void RaiseDerived()
        {
            Raise(nameof(IsOccupied));
            Raise(nameof(IsAvailable));
            Raise(nameof(DisplayStatus));
            Raise(nameof(TileNote));
        }
    }
}
