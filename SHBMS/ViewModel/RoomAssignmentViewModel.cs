using SHBMS.Model;

namespace SHBMS.ViewModel;

// A separate, small ViewModel for each ComboBox in the assignment form.
public class RoomAssignmentViewModel : BaseViewModel
{
    public string RoomTypeId { get; set; } = "";
    public string Label { get; set; } = "";

    // Only suitable ACTIVE rooms for this booked type are offered by the service.
    public List<Room> AvailableRooms { get; set; } = new();

    // Each assignment row owns this value so one dropdown does not overwrite another.
    private string? _selectedRoomId;
    public string? SelectedRoomId
    {
        get => _selectedRoomId;
        set => SetProperty(ref _selectedRoomId, value);
    }
}
