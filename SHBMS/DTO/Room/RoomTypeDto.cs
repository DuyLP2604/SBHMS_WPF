namespace SBHMS.DTO.Room;

public class RoomTypeDto
{
    public int RoomTypeId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal BasePrice { get; set; }
    public int Capacity { get; set; }
}
