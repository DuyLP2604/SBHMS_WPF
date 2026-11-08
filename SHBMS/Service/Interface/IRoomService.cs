using SHBMS.DTO.Room;

namespace SHBMS.Service.Interface;

public interface IRoomService
{
    /// <summary>
    /// Read operational room statuses and current guest names.
    /// </summary>
    Task<IEnumerable<RoomStatusDto>> GetRealTimeRoomStatusesAsync();

    /// <summary>
    /// Change operational room status after checking existing stays.
    /// </summary>
    Task<bool> UpdateRoomStatusAsync(string roomId, RoomStatusEnum newStatus);

    /// <summary>
    /// Read room types for viewing, selection and management.
    /// </summary>
    Task<IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync();

    /// <summary>
    /// Read room information with current occupancy details.
    /// </summary>
    Task<IEnumerable<RoomDto>> GetAllRoomsAsync();

    /// <summary>
    /// Create a validated room with an automatic ID.
    /// </summary>
    Task<bool> CreateRoomAsync(RoomCreateDto dto);

    /// <summary>
    /// Update a room without breaking existing reservations.
    /// </summary>
    Task<bool> UpdateRoomAsync(RoomUpdateDto dto);

    /// <summary>
    /// Delete a room only when inventory and history rules allow it.
    /// </summary>
    Task<bool> DeleteRoomAsync(string roomId);

    /// <summary>
    /// Create one of the four supported room types.
    /// </summary>
    Task<bool> CreateRoomTypeAsync(RoomTypeCreateDto dto);

    /// <summary>
    /// Update a room type while preserving booked prices.
    /// </summary>
    Task<bool> UpdateRoomTypeAsync(RoomTypeUpdateDto dto);

    /// <summary>
    /// Delete a room type without rooms or booking history.
    /// </summary>
    Task<bool> DeleteRoomTypeAsync(string roomTypeId);
}
