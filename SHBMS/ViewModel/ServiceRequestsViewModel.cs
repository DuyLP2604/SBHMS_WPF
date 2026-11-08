
using SHBMS.DTO.Service;
using SHBMS.Helper;
using SHBMS.Model;
using SHBMS.Service.Implementation;
using SHBMS.Service.Interface;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SHBMS.ViewModel;

public sealed class ServiceRequestsViewModel : DatabaseViewModel
{
    private readonly IServiceRequestManager _manager;

    private List<GuestServiceRequest> _all = new();

    public bool CanManageRequests =>
        Session.CanManageServiceRequests;

    public ObservableCollection<GuestServiceRequest> Requests
    { get; } = new();

    public ObservableCollection<Booking> Bookings
    { get; } = new();

    public ObservableCollection<SHBMS.Model.Service> Services
    { get; } = new();

    public string[] Statuses { get; } =
    {
        "Pending",
        "InProgress",
        "Resolved",
        "Rejected"
    };

    public string[] Filters { get; } =
    {
        "All",
        "Pending",
        "InProgress",
        "Resolved",
        "Rejected"
    };

    private string _searchText = "";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    private string _statusFilter = "All";

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
                ApplyFilter();
        }
    }

    private Booking? _selectedBooking;

    public Booking? SelectedBooking
    {
        get => _selectedBooking;
        set => SetProperty(ref _selectedBooking, value);
    }

    private SHBMS.Model.Service? _selectedService;

    public SHBMS.Model.Service? SelectedService
    {
        get => _selectedService;
        set => SetProperty(ref _selectedService, value);
    }

    private string _details = "";

    public string Details
    {
        get => _details;
        set => SetProperty(ref _details, value);
    }

    private GuestServiceRequest? _selectedRequest;

    public GuestServiceRequest? SelectedRequest
    {
        get => _selectedRequest;
        set
        {
            if (!SetProperty(ref _selectedRequest, value))
                return;

            SelectedStatus = value?.Status ?? "Pending";

            CommandManager.InvalidateRequerySuggested();
        }
    }

    private string _selectedStatus = "Pending";

    public string SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    public RelayCommand NewCommand { get; }
    public RelayCommand CreateCommand { get; }
    public RelayCommand UpdateStatusCommand { get; }

    public ServiceRequestsViewModel()
        : this(new ServiceRequestManager())
    {
    }

    public ServiceRequestsViewModel(
        IServiceRequestManager manager)
    {
        _manager = manager;

        NewCommand = new RelayCommand(
            _ => ClearForm(),
            _ => !IsBusy && CanManageRequests
        );

        CreateCommand = DatabaseCommand(
            CreateAsync,
            () => CanManageRequests
        );

        UpdateStatusCommand = DatabaseCommand(
            UpdateStatusAsync,
            () => CanManageRequests &&
                  SelectedRequest != null &&
                  SelectedRequest.Status != "Resolved" &&
                  SelectedRequest.Status != "Rejected"
        );
    }

    protected override async Task LoadDataAsync()
    {
        if (!CanManageRequests)
        {
            StatusMessage = "Access denied.";
            return;
        }

        var bookings =
            await _manager.GetCheckedInBookingsAsync();

        var services =
            await _manager.GetServicesAsync();

        _all = await _manager.GetRequestsAsync();

        Bookings.Clear();

        foreach (var booking in bookings)
            Bookings.Add(booking);

        Services.Clear();

        foreach (var service in services)
            Services.Add(service);

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var keyword = SearchText.Trim();

        var filtered = _all.Where(x =>
            (StatusFilter == "All" ||
             x.Status == StatusFilter) &&

            (string.IsNullOrEmpty(keyword) ||
             x.BookingId.Contains(
                 keyword,
                 StringComparison.OrdinalIgnoreCase) ||

             x.ServiceName.Contains(
                 keyword,
                 StringComparison.OrdinalIgnoreCase) ||

             x.Details.Contains(
                 keyword,
                 StringComparison.OrdinalIgnoreCase))
        );

        Requests.Clear();

        foreach (var request in filtered)
            Requests.Add(request);
    }

    private async Task CreateAsync()
    {
        if (!CanManageRequests)
        {
            StatusMessage = "Only Staff can create requests.";
            return;
        }

        if (SelectedBooking == null)
        {
            StatusMessage = "Please select a booking.";
            return;
        }

        if (SelectedService == null)
        {
            StatusMessage = "Please select a service.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Details))
        {
            StatusMessage = "Please enter request details.";
            return;
        }

        var request = new GuestServiceRequest
        {
            BookingId = SelectedBooking.BookingId,
            ServiceId = SelectedService.ServiceId,
            ServiceName = SelectedService.ServiceName,
            Details = Details.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.Now,
            HandledBy =
                Session.CurrentAccount?.EmployeeId ?? ""
        };

        await _manager.AddAsync(request);

        await LoadDataAsync();

        ClearForm();

        StatusMessage = "Request created successfully.";
    }

    private async Task UpdateStatusAsync()
    {
        if (!CanManageRequests)
        {
            StatusMessage = "Only Staff can update requests.";
            return;
        }

        if (SelectedRequest == null)
        {
            StatusMessage = "Please select a request.";
            return;
        }

        if (SelectedRequest.Status is "Resolved" or "Rejected")
        {
            StatusMessage = "This request is already closed.";
            return;
        }

        bool updated = await _manager.UpdateStatusAsync(
            SelectedRequest.Id,
            SelectedStatus,
            Session.CurrentAccount?.EmployeeId ?? ""
        );

        await LoadDataAsync();

        SelectedRequest = null;

        StatusMessage = updated
            ? "Request status updated successfully."
            : "Request not found.";
    }

    private void ClearForm()
    {
        SelectedRequest = null;
        SelectedBooking = null;
        SelectedService = null;
        SelectedStatus = "Pending";
        Details = "";
    }
}
