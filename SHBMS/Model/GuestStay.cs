namespace SHBMS.Model
{
    // Same lifecycle as BOOKING.BookingStatus: CONFIRMED -> CHECKED_IN -> CHECKED_OUT
    public enum StayStatus
    {
        Confirmed,   // booking confirmed, guest has not arrived yet
        CheckedIn,   // guest is in house
        CheckedOut
    }

    /// <summary>Result of a check-out calculation (like InvoiceSummaryDto).</summary>
    public record StayBill(int Nights, decimal RoomCharge, decimal ServiceCharge)
    {
        public decimal Total => RoomCharge + ServiceCharge;
    }

    /// <summary>
    /// One guest stay. There is no STAY table in the database yet, so a stay is
    /// a BOOKING that is being checked in/out (BookingId is empty for walk-ins).
    /// </summary>
    public class GuestStay : NotifyObject
    {
        public string StayId { get; set; } = "";
        public string BookingId { get; set; } = "";
        public string CustomerId { get; set; } = "";
        public string GuestName { get; set; } = "";
        public string RoomTypeName { get; set; } = "";
        public decimal PricePerNight { get; set; }
        public DateTime ArrivalDate { get; set; }

        private string _roomNumber = "";
        public string RoomNumber
        {
            get => _roomNumber;
            set { if (Set(ref _roomNumber, value)) Raise(nameof(RoomText)); }
        }

        private DateTime? _checkInTime;
        public DateTime? CheckInTime
        {
            get => _checkInTime;
            set => Set(ref _checkInTime, value);
        }

        private DateTime _expectedCheckOut;
        public DateTime ExpectedCheckOut
        {
            get => _expectedCheckOut;
            set => Set(ref _expectedCheckOut, value);
        }

        private DateTime? _actualCheckOut;
        public DateTime? ActualCheckOut
        {
            get => _actualCheckOut;
            set => Set(ref _actualCheckOut, value);
        }

        private decimal _serviceCharge;
        public decimal ServiceCharge
        {
            get => _serviceCharge;
            set => Set(ref _serviceCharge, value);
        }

        private StayStatus _status = StayStatus.Confirmed;
        public StayStatus Status
        {
            get => _status;
            set { if (Set(ref _status, value)) Raise(nameof(StatusText)); }
        }

        public string RoomText => _roomNumber.Length == 0 ? "—" : _roomNumber;

        public string BookingRef => BookingId.Length == 0 ? "Walk-in" : BookingId;

        public string StatusText => Status switch
        {
            StayStatus.Confirmed => "Arriving",
            StayStatus.CheckedIn => "In house",
            _ => "Checked out"
        };

        /// <summary>
        /// Confirmed  -> estimate for the booked dates
        /// CheckedIn  -> what the guest would pay if checking out at <paramref name="asOf"/>
        /// CheckedOut -> final amount
        /// </summary>
        public StayBill CalculateBill(DateTime asOf)
        {
            DateTime start;
            DateTime end;

            switch (Status)
            {
                case StayStatus.Confirmed:
                    start = ArrivalDate;
                    end = ExpectedCheckOut;
                    break;
                case StayStatus.CheckedIn:
                    start = CheckInTime ?? ArrivalDate;
                    end = asOf;
                    break;
                default:
                    start = CheckInTime ?? ArrivalDate;
                    end = ActualCheckOut ?? asOf;
                    break;
            }

            int nights = Math.Max(1, (end.Date - start.Date).Days);
            return new StayBill(nights, nights * PricePerNight, ServiceCharge);
        }
    }
}
