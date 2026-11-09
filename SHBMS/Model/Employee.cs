namespace SHBMS.Model;

public partial class Employee
{
    public string EmployeeId { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Position { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public int UserId { get; set; }

    public virtual ICollection<Booking> BookingDeletedByEmployeeNavigations { get; set; } = new List<Booking>();

    public virtual ICollection<Booking> BookingEmployees { get; set; } = new List<Booking>();

    public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();

    public virtual ICollection<Complaint> ComplaintEmployees { get; set; } = new List<Complaint>();

    public virtual ICollection<Complaint> ComplaintResolvedByEmployeeNavigations { get; set; } = new List<Complaint>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<RoomAssignment> RoomAssignments { get; set; } = new List<RoomAssignment>();

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();

    public virtual User User { get; set; } = null!;
}
