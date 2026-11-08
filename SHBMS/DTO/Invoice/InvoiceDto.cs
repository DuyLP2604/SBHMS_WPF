using SHBMS.DTO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SHBMS.DTO.Invoice;

public class InvoiceDto
{
    public string InvoiceId { get; set; }
    public string BookingId { get; set; }
    public string StayId { get; set; }
    public string CustomerName { get; set; }
    public string RoomNumber { get; set; }
    public string EmployeeId { get; set; }
    public string TotalAmount { get; set; }
    public string Rooms { get; set; }
    public string Services { get; set; }
    public DateTime CheckinDate { get; set; }
    public DateTime CheckoutDate { get; set; }
    public DateTime InvoiceDate { get; set; }
}
