using SHBMS.Helper;
namespace SHBMS.ViewModel;
public class MainWindowViewModel : BaseViewModel
{
    public string DisplayName => Session.CurrentAccount?.FullName ?? "";
    public string Role => Session.CurrentAccount?.Role ?? "";
    public bool IsAdmin => Session.IsAdmin;
    public bool CanViewRooms => Session.CanViewRooms;
    public bool CanViewBookings => Session.CanViewBookings;
    public bool CanManagePricing => Session.IsManager || Session.IsAdmin;
    private string _pageTitle = "Customer profiles";
    public string PageTitle { get => _pageTitle; set => SetProperty(ref _pageTitle, value); }
}
