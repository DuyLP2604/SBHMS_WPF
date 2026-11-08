namespace SHBMS.DTO.Auth;

public class AccountDto
{
    public int UserId { get; set; }
    public string? EmployeeId { get; set; }
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = null!;
}
