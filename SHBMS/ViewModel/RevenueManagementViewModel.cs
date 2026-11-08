using System.Collections.ObjectModel;
using System.Windows.Input;
using SHBMS.DTO.Revenue;
using SHBMS.Helper;
using SHBMS.Model;

namespace SHBMS.ViewModel;

public class RevenueManagementViewModel : BaseViewModel
{
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
    public ICommand UpdatePromoCommand { get; }
    public ICommand DeletePromoCommand { get; }
    public ICommand ClearPromoCommand { get; }

    public ObservableCollection<RoomType> RoomTypeList { get; set; } = new ObservableCollection<RoomType>();

    private RoomType? _selectedRoomType;
    public RoomType? SelectedRoomType
    {
        get { return _selectedRoomType; }
        set
        {
            if (SetProperty(ref _selectedRoomType, value) && value != null)
            {
                EditRoomPrice = value.Price;
            }
        }
    }

    private decimal _editRoomPrice = 0;
    public decimal EditRoomPrice
    {
        get { return _editRoomPrice; }
        set { SetProperty(ref _editRoomPrice, value); }
    }

    private string _weekendRate = "1.2";
    public string WeekendRate
    {
        get { return _weekendRate; }
        set { SetProperty(ref _weekendRate, value); }
    }

    public ICommand SavePricingRuleCommand { get; }
    public ICommand UpdateRoomPriceCommand { get; }

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
        UpdatePromoCommand = new RelayCommand(_ => UpdatePromotion());
        DeletePromoCommand = new RelayCommand(_ => DeletePromotion());
        ClearPromoCommand = new RelayCommand(_ => ClearPromoForm());

        SavePricingRuleCommand = new RelayCommand(_ => SaveWeekendSurcharge());
        UpdateRoomPriceCommand = new RelayCommand(_ => UpdateRoomPrice());

        InitSamplePromotions();
        LoadData();
    }

    private void InitSamplePromotions()
    {
        PromotionDto p1 = new PromotionDto
        {
            PromotionId = 1,
            Code = "WELCOME10",
            DiscountPercentage = 10,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(1),
            Description = "10% off for first-time guests",
            IsActive = true
        };
        PromotionDto p2 = new PromotionDto
        {
            PromotionId = 2,
            Code = "HOLIDAY20",
            DiscountPercentage = 20,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(2),
            Description = "20% off for holiday season bookings",
            IsActive = true
        };
        PromotionList.Add(p1);
        PromotionList.Add(p2);
    }

    public void LoadData()
    {
        LoadRoomTypesFromDb();
        LoadRevenueFromDb();
    }

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

            if (RoomTypeList.Count > 0 && SelectedRoomType == null)
            {
                SelectedRoomType = RoomTypeList[0];
            }
        }
    }

    public void UpdateRoomPrice()
    {
        if (SelectedRoomType == null)
        {
            System.Windows.MessageBox.Show("Please select a room type from the table to update its price!", "No Selection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (EditRoomPrice <= 0)
        {
            System.Windows.MessageBox.Show("Price must be greater than 0 VND!", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        using (Prn212SbhmsContext context = new Prn212SbhmsContext())
        {
            RoomType? roomInDb = context.RoomTypes.FirstOrDefault(r => r.RoomTypeId == SelectedRoomType.RoomTypeId);
            if (roomInDb != null)
            {
                roomInDb.Price = EditRoomPrice;
                context.SaveChanges();

                System.Windows.MessageBox.Show($"Price for {roomInDb.TypeName} ({roomInDb.RoomTypeId}) has been successfully updated to {EditRoomPrice:N0} VND in the database!", "Price Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                LoadRoomTypesFromDb();
            }
        }
    }

    public void SaveWeekendSurcharge()
    {
        decimal rate;
        if (!decimal.TryParse(WeekendRate, out rate) || rate < 1.0m)
        {
            System.Windows.MessageBox.Show("Weekend surcharge multiplier must be a valid number >= 1.0 (e.g. 1.2)", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        decimal sampleBase = SelectedRoomType != null ? SelectedRoomType.Price : 500000;
        decimal sampleWeekend = sampleBase * rate;

        System.Windows.MessageBox.Show($"Saved weekend surcharge multiplier: x{rate}\n\nExample preview:\nBase price: {sampleBase:N0} VND\nWeekend price: {sampleWeekend:N0} VND", "Surcharge Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    public void LoadRevenueFromDb()
    {
        using (Prn212SbhmsContext context = new Prn212SbhmsContext())
        {
            List<Invoice> invoiceList = context.Invoices.ToList();

            RevenueList.Clear();

            if (SelectedReportType == "Monthly")
            {
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

    public void AddPromotion()
    {
        if (string.IsNullOrWhiteSpace(PromoCode))
        {
            System.Windows.MessageBox.Show("Please enter a promotion code!", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (PromoDiscount <= 0 || PromoDiscount > 100)
        {
            System.Windows.MessageBox.Show("Discount percentage must be between 1% and 100%!", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
        System.Windows.MessageBox.Show($"Promotion code '{promo.Code}' with {promo.DiscountPercentage}% discount added successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

        ClearPromoForm();
    }

    public void UpdatePromotion()
    {
        if (SelectedPromotion == null)
        {
            System.Windows.MessageBox.Show("Please select a promotion from the table to update!", "No Selection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(PromoCode))
        {
            System.Windows.MessageBox.Show("Please enter a promotion code!", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        SelectedPromotion.Code = PromoCode.Trim().ToUpper();
        SelectedPromotion.DiscountPercentage = PromoDiscount;
        SelectedPromotion.StartDate = PromoStartDate ?? DateTime.Today;
        SelectedPromotion.EndDate = PromoEndDate ?? DateTime.Today.AddMonths(1);
        SelectedPromotion.Description = PromoDesc;

        int index = PromotionList.IndexOf(SelectedPromotion);
        if (index >= 0)
        {
            PromotionDto current = SelectedPromotion;
            PromotionList.RemoveAt(index);
            PromotionList.Insert(index, current);
            SelectedPromotion = current;
        }

        System.Windows.MessageBox.Show($"Promotion '{SelectedPromotion.Code}' updated successfully!", "Updated", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    public void DeletePromotion()
    {
        if (SelectedPromotion == null)
        {
            System.Windows.MessageBox.Show("Please select a promotion to delete!", "No Selection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show($"Are you sure you want to delete promotion code '{SelectedPromotion.Code}'?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            PromotionList.Remove(SelectedPromotion);
            ClearPromoForm();
        }
    }

    public void ClearPromoForm()
    {
        SelectedPromotion = null;
        PromoCode = "";
        PromoDiscount = 10;
        PromoStartDate = DateTime.Today;
        PromoEndDate = DateTime.Today.AddMonths(1);
        PromoDesc = "";
    }

    public void SwitchTab(string? tab)
    {
        if (tab == "1")
        {
            IsRevenueVisible = true;
            IsPricingVisible = false;
        }
        else if (tab == "2")
        {
            IsRevenueVisible = false;
            IsPricingVisible = true;
        }
        else
        {
            IsRevenueVisible = true;
            IsPricingVisible = true;
        }
    }
}
