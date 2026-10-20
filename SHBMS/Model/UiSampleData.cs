namespace SHBMS.Model;

public static class UiSampleData
{
    public static IReadOnlyList<UiCustomer> Customers() => new[]
    {
        new UiCustomer("CUS001", "Nguyen Minh Anh", "0901234567"),
        new UiCustomer("CUS002", "Tran Gia Bao", "0912345678"),
        new UiCustomer("CUS003", "Emily Johnson", "0987654321")
    };

    public static IReadOnlyList<UiRoomType> RoomTypes() => new[]
    {
        new UiRoomType { RoomTypeId = "RT01", Name = "Deluxe", Capacity = 2, BasePrice = 700000, TotalRooms = 3, BookingReferences = 2, AvailableRooms = 2, Description = "A comfortable room with a double bed and a city view." },
        new UiRoomType { RoomTypeId = "RT02", Name = "Family Suite", Capacity = 4, BasePrice = 1200000, TotalRooms = 2, BookingReferences = 1, AvailableRooms = 1, Description = "A spacious family suite with a separate sitting area." },
        new UiRoomType { RoomTypeId = "RT03", Name = "Standard", Capacity = 2, BasePrice = 500000, TotalRooms = 0, BookingReferences = 0, AvailableRooms = 0, Description = "A simple room type with no room assigned yet." }
    };

    public static IReadOnlyList<UiRoom> Rooms() => new[]
    {
        new UiRoom { RoomId = "101", RoomNumber = "101", RoomTypeName = "Deluxe", Capacity = 2, BasePrice = 700000, Description = "Double bed, city view" },
        new UiRoom { RoomId = "102", RoomNumber = "102", RoomTypeName = "Deluxe", Capacity = 2, BasePrice = 700000, Description = "Double bed, city view" },
        new UiRoom { RoomId = "201", RoomNumber = "201", RoomTypeName = "Family Suite", Capacity = 4, BasePrice = 1200000, Description = "Two beds, balcony" },
        new UiRoom { RoomId = "202", RoomNumber = "202", RoomTypeName = "Family Suite", Capacity = 4, BasePrice = 1200000, Status = "MAINTENANCE", Description = "Maintenance in progress" }
    };

    public static IReadOnlyList<UiBooking> Bookings() => new[]
    {
        new UiBooking { BookingId = "BK0002", CustomerId = "CUS002", CustomerName = "Tran Gia Bao", CustomerPhone = "0912345678", CheckInDate = DateTime.Today.AddDays(-1), CheckOutDate = DateTime.Today.AddDays(1), IsOverdueCheckIn = true },
        new UiBooking { BookingId = "BK0001", CustomerId = "CUS001", CustomerName = "Nguyen Minh Anh", CustomerPhone = "0901234567", CheckInDate = DateTime.Today.AddDays(1), CheckOutDate = DateTime.Today.AddDays(3), TotalRooms = 2 },
        new UiBooking { BookingId = "BK0003", CustomerId = "CUS003", CustomerName = "Emily Johnson", CustomerPhone = "0987654321", CheckInDate = DateTime.Today.AddDays(-1), CheckOutDate = DateTime.Today.AddDays(2), Status = "CHECKED_IN", ServiceSubtotal = 100000 },
        new UiBooking { BookingId = "BK0004", CustomerId = "CUS001", CustomerName = "Nguyen Minh Anh", CustomerPhone = "0901234567", CheckInDate = DateTime.Today.AddDays(-5), CheckOutDate = DateTime.Today.AddDays(-3), Status = "CHECKED_OUT", ServiceSubtotal = 100000 }
    };

    public static UiBookingDetail Detail(UiBooking booking) => new()
    {
        Booking = booking,
        CustomerEmail = "customer@example.com",
        CustomerAddress = "Can Tho, Vietnam",
        Rooms = new[] { new UiRoomLine("RT01", "Deluxe", 2, booking.TotalRooms, 700000, booking.NumberOfNights) },
        Services = booking.ServiceSubtotal == 0 ? Array.Empty<UiServiceLine>() : new[] { new UiServiceLine("Breakfast", 2, 50000, 100000) },
        Payments = new[] { new UiPayment("PAY001", booking.BookingDate, 500000, "Cash", "Staff preview", "Sample deposit") },
        Assignments = booking.Status is "CHECKED_IN" or "CHECKED_OUT" ? new[] { new UiAssignment("101", "Deluxe", "Staff preview", booking.CheckInDate.AddHours(14)) } : Array.Empty<UiAssignment>(),
        Invoices = booking.Status == "CHECKED_OUT" ? new[] { new UiInvoice("INV001", booking.CheckOutDate, booking.TotalEstimatedAmount, "Staff preview") } : Array.Empty<UiInvoice>()
    };
}
