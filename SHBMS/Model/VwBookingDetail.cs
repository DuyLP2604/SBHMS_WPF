namespace SHBMS.Model;

public partial class VwBookingDetail
{
    public int BookingDetailId { get; set; }

    public string BookingId { get; set; } = null!;

    public string RoomTypeId { get; set; } = null!;

    public string TypeName { get; set; } = null!;

    public int Capacity { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public int? NumberOfNights { get; set; }

    public decimal? LineTotal { get; set; }
}
