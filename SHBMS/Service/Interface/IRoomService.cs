namespace SBHMS.Services.Interfaces
{
    public interface IRoomService
    {
        // [UC-13] View Room Status
        Task<IEnumerable<RoomStatusDto>> GetRealTimeRoomStatusesAsync();

        // [UC-14] Update Room Status
        Task<bool> UpdateRoomStatusAsync(int roomId, RoomStatusEnum newStatus);

        // [UC-20] View Rooms & Room Types
        Task<IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync();
        Task<IEnumerable<RoomDto>> GetAllRoomsAsync();

        // [UC-21] Manage Rooms & Room Types (Manager)
        Task<bool> CreateRoomAsync(RoomCreateDto dto);
        Task<bool> UpdateRoomAsync(RoomUpdateDto dto);
        Task<bool> CreateRoomTypeAsync(RoomTypeCreateDto dto);
        Task<bool> UpdateRoomTypeAsync(RoomTypeUpdateDto dto);
    }
}