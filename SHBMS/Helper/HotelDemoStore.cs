using SBHMS.DTO.Room;
using SHBMS.Model;
using System.Collections.ObjectModel;

namespace SHBMS.Helper
{
    /// <summary>
    /// TEMPORARY in-memory data shared by the "Guest stays" and "Room status" screens,
    /// so checking a guest in immediately turns the room to "Occupied" on the other screen.
    ///
    /// When the database is connected, replace the calls to this class with the services
    /// (IGuestStayService / IRoomService) - the screens only talk to these few methods.
    /// </summary>
    public static class HotelDemoStore
    {
        private static int _nextStayNumber = 8;

        public static ObservableCollection<RoomInfo> Rooms { get; } = new();
        public static ObservableCollection<GuestStay> Stays { get; } = new();
        public static List<CustomerProfile> Customers { get; } = new();

        static HotelDemoStore()
        {
            SeedCustomers();
            SeedRooms();
            SeedStays();
        }

        // =====================================================
        // OPERATIONS (what the services will do later)
        // =====================================================
        public static string NextStayId() => $"ST{_nextStayNumber++:0000}";

        public static List<RoomInfo> GetAvailableRooms(string? roomTypeName = null)
            => Rooms
                .Where(r => r.IsAvailable &&
                            (roomTypeName == null || r.RoomTypeName == roomTypeName))
                .OrderBy(r => r.RoomNumber)
                .ToList();

        /// <summary>[UC-07] Put the guest into the room.</summary>
        public static void CheckIn(GuestStay stay, RoomInfo room, DateTime expectedCheckOut)
        {
            stay.RoomNumber = room.RoomNumber;
            stay.CheckInTime = DateTime.Now;
            stay.ExpectedCheckOut = expectedCheckOut.Date.AddHours(12); // standard 12:00 check-out
            stay.Status = StayStatus.CheckedIn;

            room.ExpectedCheckOut = stay.ExpectedCheckOut;
            room.CurrentGuestName = stay.GuestName;
        }

        /// <summary>[UC-08] Close the stay, free the room and return the bill.</summary>
        public static StayBill CheckOut(GuestStay stay)
        {
            var now = DateTime.Now;
            var room = Rooms.FirstOrDefault(r => r.RoomNumber == stay.RoomNumber);

            stay.ActualCheckOut = now;
            stay.Status = StayStatus.CheckedOut;

            if (room != null)
            {
                room.ExpectedCheckOut = null;
                room.CurrentGuestName = "";
            }

            return stay.CalculateBill(now);
        }

        // =====================================================
        // SAMPLE DATA
        // =====================================================
        private static void SeedCustomers()
        {
            Customers.Add(new CustomerProfile { CustomerID = "CUS001", FullName = "Nguyen Minh Anh", IdentityNumber = "079201001234", Phone = "0901234567", Nationality = "Vietnamese" });
            Customers.Add(new CustomerProfile { CustomerID = "CUS002", FullName = "Tran Gia Bao", IdentityNumber = "079202005678", Phone = "0912345678", Nationality = "Vietnamese" });
            Customers.Add(new CustomerProfile { CustomerID = "CUS003", FullName = "Emily Johnson", IdentityNumber = "P12345678", Phone = "0987654321", Nationality = "British" });
            Customers.Add(new CustomerProfile { CustomerID = "CUS004", FullName = "Le Hoang Nam", IdentityNumber = "092203004321", Phone = "0933111222", Nationality = "Vietnamese" });
            Customers.Add(new CustomerProfile { CustomerID = "CUS005", FullName = "Pham Thu Ha", IdentityNumber = "092199008765", Phone = "0944333555", Nationality = "Vietnamese" });
        }

        private static void SeedRooms()
        {
            int id = 1;

            void Add(string typeId, string typeName, int capacity, decimal price, params string[] numbers)
            {
                foreach (var number in numbers)
                {
                    Rooms.Add(new RoomInfo
                    {
                        RoomId = $"R{id++:00}",
                        RoomNumber = number,
                        RoomTypeId = typeId,
                        RoomTypeName = typeName,
                        Capacity = capacity,
                        PricePerNight = price
                    });
                }
            }

            Add("RT01", "Standard", 2, 500_000m, "101", "102", "103", "104", "105");
            Add("RT02", "Deluxe", 3, 800_000m, "201", "202", "203", "204", "205");
            Add("RT03", "Suite", 4, 1_500_000m, "301", "302", "303");

            Rooms.First(r => r.RoomNumber == "102").Status = RoomStatusEnum.Maintenance;
            Rooms.First(r => r.RoomNumber == "303").Status = RoomStatusEnum.Inactive;
        }

        private static void SeedStays()
        {
            var today = DateTime.Today;

            // ---- Guests in house ----
            AddInHouse("ST0001", "BK0001", "CUS003", "201", today.AddDays(-2).AddHours(14), today.AddDays(1), 250_000m);
            AddInHouse("ST0002", "BK0002", "CUS001", "101", today.AddDays(-1).AddHours(15), today, 0m);
            AddInHouse("ST0003", "BK0003", "CUS002", "301", today.AddDays(-1).AddHours(20), today.AddDays(3), 480_000m);

            // ---- Arrivals (confirmed bookings) ----
            Stays.Add(NewStay("ST0004", "BK0004", "CUS004", "Standard", 500_000m, today, today.AddDays(2), 0m));
            Stays.Add(NewStay("ST0005", "BK0005", "CUS005", "Deluxe", 800_000m, today, today.AddDays(1), 0m));
            Stays.Add(NewStay("ST0006", "BK0006", "CUS001", "Suite", 1_500_000m, today.AddDays(1), today.AddDays(4), 0m));

            // ---- History ----
            var done = NewStay("ST0007", "BK0007", "CUS002", "Standard", 500_000m, today.AddDays(-4), today.AddDays(-1), 120_000m);
            done.RoomNumber = "104";
            done.CheckInTime = today.AddDays(-4).AddHours(13);
            done.ActualCheckOut = today.AddDays(-1).AddHours(11);
            done.Status = StayStatus.CheckedOut;
            Stays.Add(done);
        }

        private static GuestStay NewStay(string stayId, string bookingId, string customerId,
            string roomType, decimal price, DateTime arrival, DateTime checkOut, decimal serviceCharge)
        {
            return new GuestStay
            {
                StayId = stayId,
                BookingId = bookingId,
                CustomerId = customerId,
                GuestName = Customers.First(c => c.CustomerID == customerId).FullName,
                RoomTypeName = roomType,
                PricePerNight = price,
                ArrivalDate = arrival.Date,
                ExpectedCheckOut = checkOut.Date.AddHours(12),
                ServiceCharge = serviceCharge,
                Status = StayStatus.Confirmed
            };
        }

        private static void AddInHouse(string stayId, string bookingId, string customerId,
            string roomNumber, DateTime checkIn, DateTime checkOut, decimal serviceCharge)
        {
            var room = Rooms.First(r => r.RoomNumber == roomNumber);
            var stay = NewStay(stayId, bookingId, customerId, room.RoomTypeName,
                room.PricePerNight, checkIn, checkOut, serviceCharge);

            stay.RoomNumber = room.RoomNumber;
            stay.CheckInTime = checkIn;
            stay.Status = StayStatus.CheckedIn;
            Stays.Add(stay);

            room.ExpectedCheckOut = stay.ExpectedCheckOut;
            room.CurrentGuestName = stay.GuestName;
        }
    }
}
