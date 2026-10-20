using SBHMS.DTO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SHBMS.DTO.Invoice;

public class InvoiceDto
{
    public int InvoiceId { get; set; }
    public int StayId { get; set; }

    public string CustomerName { get; set; }
    public string RoomNumber { get; set; }

    public decimal RoomCharge { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsPaid { get; set; }

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }

    public List<ServiceOrderDto> ServiceOrders { get; set; } = new List<ServiceOrderDto>();

    public string DisplayService
    {
        get
        {
            if (ServiceOrders != null && ServiceOrders.Count > 0)
            {
                return string.Join(", ", ServiceOrders.Select(s => s.ServiceName));
            }
            return "-";
        }
    }
}