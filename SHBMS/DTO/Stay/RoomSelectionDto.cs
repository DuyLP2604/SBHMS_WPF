namespace SHBMS.DTO.Stay;

// One selected physical room for one room requested by the booking.
public class RoomSelectionDto
{
    public string RoomTypeId { get; set; } = "";
    public string RoomId { get; set; } = "";
}
