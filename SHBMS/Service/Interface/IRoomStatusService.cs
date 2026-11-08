using SHBMS.DTO.Room;
namespace SHBMS.Service.Interface;

// [FE-07] Monitor Room Status: Manager and Staff view, only Staff updates.
public interface IRoomStatusService
{
    Task<List<RoomStatusDto>> GetRoomStatusesAsync();
    Task UpdateRoomStatusAsync(string roomId, RoomStatusEnum newStatus);
}
