using SHBMS.DTO.Auth;
namespace SHBMS.Helper;

public static class Session
{
    public static AccountDto? CurrentAccount { get; private set; }
    public static bool IsLoggedIn => CurrentAccount != null;
    public static bool IsManager => CurrentAccount?.Role == "Manager";
    public static bool IsAdmin => CurrentAccount?.Role == "Admin";
    public static bool IsStaff => CurrentAccount?.Role == "Staff";
    public static bool CanViewRooms => CurrentAccount?.Role is "Staff" or "Manager";
    public static bool CanViewBookings => CurrentAccount?.Role is "Staff" or "Manager";
    public static bool CanViewInvoices => CurrentAccount?.Role is "Staff" or "Manager";
    public static bool CanManageCustomerProfiles => IsStaff;
    public static bool CanManageGuestStays => IsStaff;
    public static bool CanManageBookingServices => IsStaff;
    public static bool CanManageServiceRequests => IsStaff;
    public static bool CanViewServiceAvailability => IsStaff || IsManager;
    public static bool CanViewGuestFeedback => IsStaff || IsManager;
    public static bool CanResolveGuestFeedback => IsStaff;
    public static bool CanManagePricing => IsManager;

    // Use the same permissions for menu visibility and page navigation.
    // Unknown pages and accounts without a supported role have no access.
    public static bool CanAccessPage(string key) => key switch
    {
        "Customers" => CanManageCustomerProfiles,
        "Bookings" => CanViewBookings,
        "CheckIn" or "CheckOut" or "Stays" => CanManageGuestStays,
        "RoomStatus" or "Rooms" or "RoomTypes" => CanViewRooms,
        "ServiceRequests" => CanManageServiceRequests,
        "ServiceAvailability" => CanViewServiceAvailability,
        "BookingServices" => CanManageBookingServices,
        "Feedback" => CanViewGuestFeedback,
        "Invoices" => CanViewInvoices,
        "Revenue" => CanManagePricing,
        "Staff" => IsAdmin,
        _ => false
    };
    public static string? GetRole() => CurrentAccount?.Role;
    public static void SetAccount(AccountDto account) => CurrentAccount = account;
    public static void Clear() => CurrentAccount = null;
}
