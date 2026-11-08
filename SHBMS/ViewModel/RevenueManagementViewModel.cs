using System.Collections.ObjectModel;
using System.Windows.Input;
using SHBMS.DTO.Revenue;
using SHBMS.Helper;
using SHBMS.Model;

namespace SHBMS.ViewModel;

public class RevenueManagementViewModel : BaseViewModel
{
    // ==========================================
    // 1. TAB SWITCHING (DUAL FUNCTION NAVIGATION)
    // ==========================================
    private bool _isRevenueVisible = true;
    public bool IsRevenueVisible
    {
        get { return _isRevenueVisible; }
        set { SetProperty(ref _isRevenueVisible, value); }
    }

    private bool _isPricingVisible = true;
    public bool IsPricingVisible
    {
        get { return _isPricingVisible; }
        set { SetProperty(ref _isPricingVisible, value); }
    }

    public ICommand SwitchTabCommand { get; }

    // ==========================================
    // 2. REVENUE REPORTS (LOADED FROM DB INVOICE)
    // ==========================================
    public ObservableCollection<RevenueReportDto> RevenueList { get; set; } = new ObservableCollection<RevenueReportDto>();
    public List<string> ReportTypes { get; set; } = new List<string> { "Daily", "Monthly" };

    private string _selectedReportType = "Daily";
    public string SelectedReportType
    {
        get { return _selectedReportType; }
        set
        {
            if (SetProperty(ref _selectedReportType, value))
            {
                if (ReportDate != null)
                {
                    FilterRevenueFromDb();
                }
                else
                {
                    LoadRevenueFromDb();
                }
            }
        }
    }

    private DateTime? _reportDate = DateTime.Today;
    public DateTime? ReportDate
    {
        get { return _reportDate; }
        set { SetProperty(ref _reportDate, value); }
    }

    public ICommand ViewReportCommand { get; }
    public ICommand ResetReportCommand { get; }

    // ==========================================
    // 3. PROMOTIONS MANAGEMENT
    // ==========================================
    public ObservableCollection<PromotionDto> PromotionList { get; set; } = new ObservableCollection<PromotionDto>();

    private PromotionDto? _selectedPromotion;
    public PromotionDto? SelectedPromotion
    {
        get { return _selectedPromotion; }
        set
        {
            if (SetProperty(ref _selectedPromotion, value) && value != null)
            {
                PromoCode = value.Code;
                PromoDiscount = value.DiscountPercentage;
                PromoStartDate = value.StartDate;
                PromoEndDate = value.EndDate;
                PromoDesc = value.Description;
            }
        }
    }

    private string _promoCode = "";
    public string PromoCode
    {
        get { return _promoCode; }
        set { SetProperty(ref _promoCode, value); }
    }

    private decimal _promoDiscount = 10;
    public decimal PromoDiscount
    {
        get { return _promoDiscount; }
        set { SetProperty(ref _promoDiscount, value); }
    }

    private DateTime? _promoStartDate = DateTime.Today;
    public DateTime? PromoStartDate
    {
        get { return _promoStartDate; }
        set { SetProperty(ref _promoStartDate, value); }
    }

    private DateTime? _promoEndDate = DateTime.Today.AddMonths(1);
    public DateTime? PromoEndDate
    {
        get { return _promoEndDate; }
        set { SetProperty(ref _promoEndDate, value); }
    }

    private string _promoDesc = "";
    public string PromoDesc
    {
        get { return _promoDesc; }
        set { SetProperty(ref _promoDesc, value); }
    }

    public ICommand AddPromoCommand { get; }

    // ==========================================
    // 4. ROOM PRICING (LOADED FROM DB ROOM_TYPE)
    // ==========================================
    public ObservableCollection<RoomType> RoomTypeList { get; set; } = new ObservableCollection<RoomType>();

    private string _weekendRate = "1.2";
    public string WeekendRate
    {
        get { return _weekendRate; }
        set { SetProperty(ref _weekendRate, value); }
    }

    public ICommand SavePricingRuleCommand { get; }

    // ==========================================
    // CONSTRUCTOR
    // ==========================================
    public RevenueManagementViewModel()
    {
        SwitchTabCommand = new RelayCommand(param => SwitchTab(param?.ToString()));
        ViewReportCommand = new RelayCommand(_ => FilterRevenueFromDb());
        ResetReportCommand = new RelayCommand(_ =>
        {
            ReportDate = null;
            LoadRevenueFromDb();
        });
        AddPromoCommand = new RelayCommand(_ => AddPromotion());
        SavePricingRuleCommand = new RelayCommand(_ =>
        {
            System.Windows.MessageBox.Show("Saved weekend surcharge multiplier: x" + WeekendRate, "Configuration Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        });

        // Load data synchronously
        LoadData();
    }

    // ==========================================
    // DATA LOADING (SYNCHRONOUS, NO ASYNC, NO VAR)
    // ==========================================
    public void LoadData()
    {
        LoadRoomTypesFromDb();
        LoadRevenueFromDb();
    }

    // 1. Load room types from database
    public void LoadRoomTypesFromDb()
    {
        using (Prn212SbhmsContext context = new Prn212SbhmsContext())
        {
            List<RoomType> roomList = context.RoomTypes.ToList();

            RoomTypeList.Clear();
            foreach (RoomType item in roomList)
            {
                RoomTypeList.Add(item);
            }
        }
    }

    // 2. Load revenue from database grouped by Daily or Monthly
    public void LoadRevenueFromDb()
    {
        using (Prn212SbhmsContext context = new Prn212SbhmsContext())
        {
            List<Invoice> invoiceList = context.Invoices.ToList();

            RevenueList.Clear();

            if (SelectedReportType == "Monthly")
            {
                // Group invoices by Year and Month
                IEnumerable<IGrouping<string, Invoice>> monthlyGroups = invoiceList
                    .GroupBy(inv => inv.InvoiceDate.ToString("MM/yyyy"));

                foreach (IGrouping<string, Invoice> group in monthlyGroups)
                {
                    decimal totalRevenue = group.Sum(inv => inv.TotalAmount);
                    int invoiceCount = group.Count();
                    Invoice firstInvoice = group.First();

                    RevenueReportDto report = new RevenueReportDto();
                    report.ReportDate = new DateTime(firstInvoice.InvoiceDate.Year, firstInvoice.InvoiceDate.Month, 1);
                    report.Period = group.Key;
                    report.RoomRevenue = totalRevenue;
                    report.ServiceRevenue = 0;
                    report.TotalCheckIns = invoiceCount;
                    report.TotalCheckOuts = invoiceCount;

                    RevenueList.Add(report);
                }
            }
            else
            {
                // Group invoices by Date (Daily)
                IEnumerable<IGrouping<DateTime, Invoice>> dailyGroups = invoiceList
                    .GroupBy(inv => inv.InvoiceDate.Date);

                foreach (IGrouping<DateTime, Invoice> group in dailyGroups)
                {
                    decimal totalRevenue = group.Sum(inv => inv.TotalAmount);
                    int invoiceCount = group.Count();

                    RevenueReportDto report = new RevenueReportDto();
                    report.ReportDate = group.Key;
                    report.Period = group.Key.ToString("dd/MM/yyyy");
                    report.RoomRevenue = totalRevenue;
                    report.ServiceRevenue = 0;
                    report.TotalCheckIns = invoiceCount;
                    report.TotalCheckOuts = invoiceCount;

                    RevenueList.Add(report);
                }
            }
        }
    }

    // 3. Filter revenue by selected date / month
    public void FilterRevenueFromDb()
    {
        if (ReportDate == null)
        {
            LoadRevenueFromDb();
            return;
        }

        DateTime targetDate = ReportDate.Value.Date;

        using (Prn212SbhmsContext context = new Prn212SbhmsContext())
        {
            List<Invoice> filteredInvoices;

            if (SelectedReportType == "Monthly")
            {
                filteredInvoices = context.Invoices
                    .Where(inv => inv.InvoiceDate.Year == targetDate.Year && inv.InvoiceDate.Month == targetDate.Month)
                    .ToList();
            }
            else
            {
                filteredInvoices = context.Invoices
                    .Where(inv => inv.InvoiceDate.Date == targetDate)
                    .ToList();
            }

            RevenueList.Clear();

            if (filteredInvoices.Count > 0)
            {
                decimal totalRevenue = filteredInvoices.Sum(inv => inv.TotalAmount);

                RevenueReportDto report = new RevenueReportDto();
                report.ReportDate = targetDate;
                if (SelectedReportType == "Monthly")
                {
                    report.Period = targetDate.ToString("MM/yyyy");
                }
                else
                {
                    report.Period = targetDate.ToString("dd/MM/yyyy");
                }

                report.RoomRevenue = totalRevenue;
                report.ServiceRevenue = 0;
                report.TotalCheckIns = filteredInvoices.Count;
                report.TotalCheckOuts = filteredInvoices.Count;

                RevenueList.Add(report);
            }
        }
    }

    // 4. Add promotion to list
    public void AddPromotion()
    {
        if (string.IsNullOrWhiteSpace(PromoCode))
        {
            System.Windows.MessageBox.Show("Please enter a promotion code!", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        PromotionDto promo = new PromotionDto();
        promo.PromotionId = PromotionList.Count + 1;
        promo.Code = PromoCode.Trim().ToUpper();
        promo.DiscountPercentage = PromoDiscount;
        promo.StartDate = PromoStartDate ?? DateTime.Today;
        promo.EndDate = PromoEndDate ?? DateTime.Today.AddMonths(1);
        promo.Description = PromoDesc;
        promo.IsActive = true;

        PromotionList.Add(promo);

        // Reset form inputs
        PromoCode = "";
        PromoDesc = "";
    }

    // 5. Tab switcher between the 2 functions
    public void SwitchTab(string? tab)
    {
        if (tab == "1") // 1. Revenue reports
        {
            IsRevenueVisible = true;
            IsPricingVisible = false;
        }
        else if (tab == "2") // 2. Promotions & pricing
        {
            IsRevenueVisible = false;
            IsPricingVisible = true;
        }
        else // View all
        {
            IsRevenueVisible = true;
            IsPricingVisible = true;
        }
    }
}
