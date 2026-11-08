using SHBMS.Model;

namespace SHBMS.ViewModel;

// A separate, small ViewModel for each ComboBox in the assignment form.
public class RoomAssignmentViewModel : BaseViewModel
{
    public string RoomTypeId { get; set; } = "";
    public string Label { get; set; } = "";
    public List<Room> AvailableRooms { get; set; } = new();

    private string? _selectedRoomId;
    public string? SelectedRoomId
    {
        get => _selectedRoomId;
        set => SetProperty(ref _selectedRoomId, value);
    }
}
