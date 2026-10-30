namespace SHBMS.Model;

public partial class RoomType
{
    public string RoomTypeId { get; set; } = null!;

    public string TypeName { get; set; } = null!;

    public int Capacity { get; set; }

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
