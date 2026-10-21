namespace SHBMS.Model
{
    public class StaffAccountModel
    {
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "Staff";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}
