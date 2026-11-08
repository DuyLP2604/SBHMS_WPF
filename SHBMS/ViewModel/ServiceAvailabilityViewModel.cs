
using SHBMS.DTO.Service;
using SHBMS.Helper;
using SHBMS.Service.Implementation;
using SHBMS.Service.Interface;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SHBMS.ViewModel;

public class ServiceAvailabilityViewModel : DatabaseViewModel
{
    private readonly IServiceAvailabilityService _service;
    private List<ServiceAvailabilityDto> _all = new();

    public ObservableCollection<ServiceAvailabilityDto> Services
    { get; } = new();

    public bool CanView =>
        Session.GetRole() is "Staff" or "Manager";

    public bool CanManage => Session.IsStaff;

    public string[] StatusFilters { get; } =
    {
        "All",
        "Available",
        "Unavailable"
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

    private ServiceAvailabilityDto? _selectedService;

    public ServiceAvailabilityDto? SelectedService
    {
        get => _selectedService;
        set
        {
            if (!SetProperty(ref _selectedService, value))
                return;

            SelectedAvailability = value?.IsAvailable ?? false;

            StockQuantityText =
                value?.StockQuantity?.ToString() ?? "";

            CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool _selectedAvailability;

    public bool SelectedAvailability
    {
        get => _selectedAvailability;
        set => SetProperty(ref _selectedAvailability, value);
    }

    private string _stockQuantityText = "";

    public string StockQuantityText
    {
        get => _stockQuantityText;
        set => SetProperty(ref _stockQuantityText, value);
    }

    public int TotalServices => Services.Count;

    public int AvailableServices =>
        Services.Count(s => s.IsAvailable);

    public int UnavailableServices =>
        Services.Count(s => !s.IsAvailable);

    public RelayCommand ResetCommand { get; }

    public RelayCommand UpdateCommand { get; }

    public ServiceAvailabilityViewModel()
        : this(new ServiceAvailabilityService())
    {
    }

    public ServiceAvailabilityViewModel(
        IServiceAvailabilityService service)
    {
        _service = service;

        ResetCommand = new RelayCommand(
            _ =>
            {
                SearchText = "";
                StatusFilter = "All";
            },
            _ => !IsBusy
        );

        UpdateCommand = DatabaseCommand(
            UpdateAsync,
            () => CanManage && SelectedService != null
        );
    }

    protected override async Task LoadDataAsync()
    {
        if (!CanView)
            throw new UnauthorizedAccessException(
                "You do not have permission to view services.");

        _all = await _service.GetServicesAsync();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        string keyword = SearchText.Trim();

        var filtered = _all.Where(s =>
            (StatusFilter == "All" ||
             s.AvailabilityStatus == StatusFilter) &&

            (string.IsNullOrWhiteSpace(keyword) ||
             Contains(s.ServiceId, keyword) ||
             Contains(s.ServiceName, keyword))
        );

        Services.Clear();

        foreach (var item in filtered)
            Services.Add(item);

        OnPropertyChanged(nameof(TotalServices));
        OnPropertyChanged(nameof(AvailableServices));
        OnPropertyChanged(nameof(UnavailableServices));
    }

    private async Task UpdateAsync()
    {
        if (!CanManage)
            throw new UnauthorizedAccessException(
                "Only Staff can update service availability.");

        if (SelectedService == null)
        {
            StatusMessage = "Please select a service.";
            return;
        }

        int? stockQuantity = null;

        if (!string.IsNullOrWhiteSpace(StockQuantityText))
        {
            if (!int.TryParse(
                StockQuantityText.Trim(),
                out int quantity))
            {
                StatusMessage = "Invalid stock quantity.";
                return;
            }

            if (quantity < 0)
            {
                StatusMessage = "Stock cannot be negative.";
                return;
            }

            stockQuantity = quantity;
        }

        string serviceId = SelectedService.ServiceId;

        bool updated = await _service.UpdateAvailabilityAsync(
            serviceId,
            SelectedAvailability,
            stockQuantity
        );

        if (!updated)
        {
            StatusMessage = "Service not found.";
            return;
        }

        // Reload directly from SQL Server
        await LoadDataAsync();

        SelectedService = Services
            .FirstOrDefault(s => s.ServiceId == serviceId);

        StatusMessage = "Service availability updated successfully.";
    }
}
