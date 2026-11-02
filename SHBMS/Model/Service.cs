using System;
using System.Collections.Generic;

namespace SHBMS.Model;

public partial class Service
{
    public string ServiceId { get; set; } = null!;

    public string ServiceName { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
}
