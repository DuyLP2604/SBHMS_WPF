using SBHMS.DTO.Room;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation
{
    public class RoomService : IRoomService
    {
        private readonly PRN212_SBHMSContext _context;
        public RoomService(PRN212_SBHMSContext context) { _context = context; }

        public Task<IEnumerable<RoomStatusDto>> GetRealTimeRoomStatusesAsync() => throw new NotImplementedException();
        public Task<bool> UpdateRoomStatusAsync(int roomId, RoomStatusEnum newStatus) => throw new NotImplementedException();
        public Task<IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync() => throw new NotImplementedException();
        public Task<IEnumerable<RoomDto>> GetAllRoomsAsync() => throw new NotImplementedException();
        public Task<bool> CreateRoomAsync(RoomCreateDto dto) => throw new NotImplementedException();
        public Task<bool> UpdateRoomAsync(RoomUpdateDto dto) => throw new NotImplementedException();
        public Task<bool> CreateRoomTypeAsync(RoomTypeCreateDto dto) => throw new NotImplementedException();
        public Task<bool> UpdateRoomTypeAsync(RoomTypeUpdateDto dto) => throw new NotImplementedException();
    }
}