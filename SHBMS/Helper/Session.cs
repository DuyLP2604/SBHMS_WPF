using SHBMS.DTO.Auth;

namespace SHBMS.Helper;

public static class Session
{
    public static AccountDto? CurrentAccount { get; private set; }

    public static bool IsLoggedIn => CurrentAccount != null;
    public static bool IsManager => CurrentAccount?.Role == "Manager";
    public static bool IsAdmin => CurrentAccount?.Role == "Admin";
    public static bool CanViewRooms => CurrentAccount?.Role is "Staff" or "Manager";
    public static bool CanViewBookings => CurrentAccount?.Role is "Staff" or "Manager";

    public static string? GetRole() => CurrentAccount?.Role;

    public static void SetAccount(AccountDto account) => CurrentAccount = account;

    public static void Clear() => CurrentAccount = null;
}