using SHBMS.DTO.Room;
namespace SHBMS.Service.Interface;
public interface IRoomService
{
    Task<IEnumerable<RoomStatusDto>> GetRealTimeRoomStatusesAsync();
    Task<bool> UpdateRoomStatusAsync(string roomId, RoomStatusEnum newStatus);
    Task<IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync();
    Task<IEnumerable<RoomDto>> GetAllRoomsAsync();
    Task<bool> CreateRoomAsync(RoomCreateDto dto);
    Task<bool> UpdateRoomAsync(RoomUpdateDto dto);
    Task<bool> DeleteRoomAsync(string roomId);
    Task<bool> CreateRoomTypeAsync(RoomTypeCreateDto dto);
    Task<bool> UpdateRoomTypeAsync(RoomTypeUpdateDto dto);
    Task<bool> DeleteRoomTypeAsync(string roomTypeId);
}
