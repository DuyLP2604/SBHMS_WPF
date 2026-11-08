using System;
using System.Collections.Generic;

namespace SHBMS.Model;

public partial class Customer
{
    public string CustomerId { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? IdentityNumber { get; set; }

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? NationalityId { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();

    public virtual Nationality? Nationality { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? NationalityName => Nationality?.NationalityName;
}
