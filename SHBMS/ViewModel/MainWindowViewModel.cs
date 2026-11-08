using SHBMS.Helper;
namespace SHBMS.ViewModel;
public class MainWindowViewModel : BaseViewModel
{
    public string DisplayName => Session.CurrentAccount?.FullName ?? "";
    public string Role => Session.CurrentAccount?.Role ?? "";
    public bool IsAdmin => Session.IsAdmin;
    public bool CanViewRooms => Session.CanViewRooms;
    public bool CanViewBookings => Session.CanViewBookings;
    public bool CanViewInvoices => Session.CanViewInvoices;
    public bool CanManagePricing => Session.CanManagePricing;
    public bool CanViewServiceAvailability => Session.CanViewServiceAvailability;
    public bool CanManageCustomerProfiles => Session.CanManageCustomerProfiles;
    public bool CanManageGuestStays => Session.CanManageGuestStays;
    public bool CanManageBookingServices => Session.CanManageBookingServices;
    public bool CanViewGuestFeedback => Session.CanViewGuestFeedback;
    public bool IsStaff => Session.IsStaff;
    public bool CanManageServiceRequests => Session.CanManageServiceRequests;

    private string _pageTitle = "Workspace";
    public string PageTitle { get => _pageTitle; set => SetProperty(ref _pageTitle, value); }
}
