namespace SHBMS.DTO.Room;
public class RoomTypeCreateDto
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal BasePrice { get; set; }
    public int Capacity { get; set; }
    public string? RoomTypeImage { get; set; }
}
