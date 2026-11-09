namespace SHBMS.Model;

public partial class BookingDetail
{
    public int BookingDetailId { get; set; }

    public string BookingId { get; set; } = null!;

    public string RoomTypeId { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual ICollection<RoomAssignment> RoomAssignments { get; set; } = new List<RoomAssignment>();

    public virtual RoomType RoomType { get; set; } = null!;
}
