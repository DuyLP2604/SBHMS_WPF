using SHBMS.DTO.Service;
using SHBMS.Helper;
using SHBMS.Service.Implementation;
using SHBMS.Service.Interface;
using System.Collections.ObjectModel;
using System.Windows.Input;
namespace SHBMS.ViewModel;

// Processing only. New orders are placed on Booking services.
public sealed class ServiceRequestsViewModel : DatabaseViewModel
{
    private readonly IServiceRequestManager _manager;
    private List<GuestServiceRequest> _all = new();
    public bool CanManageRequests => Session.CanManageServiceRequests;
    public ObservableCollection<GuestServiceRequest> Requests { get; } = new();
    public string[] Statuses { get; } = { "Pending", "InProgress", "Resolved", "Rejected" };
    public string[] Filters { get; } = { "All", "Pending", "InProgress", "Resolved", "Rejected" };
    public string Summary => $"{Requests.Count} service orders";
    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) ApplyFilter(); }
    }
    private string _statusFilter = "All";
    public string StatusFilter
    {
        get => _statusFilter;
        set { if (SetProperty(ref _statusFilter, value)) ApplyFilter(); }
    }
    private GuestServiceRequest? _selectedRequest;
    public GuestServiceRequest? SelectedRequest
    {
        get => _selectedRequest;
        set
        {
            SetProperty(ref _selectedRequest, value);
            SelectedStatus = value?.Status ?? "Pending";
            CommandManager.InvalidateRequerySuggested();
        }
    }
    private string _selectedStatus = "Pending";
    public string SelectedStatus
    {
        get => _selectedStatus;
        set { SetProperty(ref _selectedStatus, value); CommandManager.InvalidateRequerySuggested(); }
    }
    public RelayCommand UpdateStatusCommand { get; }
    public ServiceRequestsViewModel() : this(new ServiceRequestManager()) { }
    public ServiceRequestsViewModel(IServiceRequestManager manager)
    {
        _manager = manager;
        UpdateStatusCommand = DatabaseCommand(UpdateStatusAsync, () => CanManageRequests
            && SelectedRequest != null && (SelectedRequest.Status == "Pending" || SelectedRequest.Status == "InProgress")
            && SelectedStatus != "Pending" && SelectedStatus != SelectedRequest.Status);
    }
    protected override async Task LoadDataAsync()
    {
        if (!CanManageRequests) throw new UnauthorizedAccessException("Only Staff can process service requests.");
        _all = await _manager.GetRequestsAsync();
        ApplyFilter();
    }
    private void ApplyFilter()
    {
        int? selectedId = SelectedRequest?.Id;
        string keyword = SearchText.Trim();
        var filtered = _all.Where(r => (StatusFilter == "All" || r.Status == StatusFilter)
            && (keyword.Length == 0 || Contains(r.BookingId, keyword) || Contains(r.CustomerName, keyword)
                || Contains(r.CustomerId, keyword) || Contains(r.ServiceName, keyword) || Contains(r.Details, keyword)));
        Requests.Clear(); foreach (var request in filtered) Requests.Add(request);
        SelectedRequest = Requests.FirstOrDefault(r => r.Id == selectedId);
        OnPropertyChanged(nameof(Summary));
    }
    private async Task UpdateStatusAsync()
    {
        if (SelectedRequest == null) return;
        bool updated = await _manager.UpdateStatusAsync(SelectedRequest.Id, SelectedStatus,
            Session.CurrentAccount?.EmployeeId ?? "");
        await LoadDataAsync();
        Notify(updated ? "Service status updated. Only completed orders are charged." : "Service order no longer exists.",
            updated ? "success" : "warning");
    }
}
