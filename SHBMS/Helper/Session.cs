using SHBMS.DTO.Auth;

namespace SHBMS.Helper
{
    public static class Session
    {
        public static AccountDTO? CurrentUser { get; set; }
        public static bool IsAdmin => CurrentUser?.Role == "Admin";
        public static bool IsManager => CurrentUser?.Role == "Manager";
    }
}