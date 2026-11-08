using Microsoft.EntityFrameworkCore;
using SHBMS.DTO.Invoice;
using SHBMS.Helper;
using SHBMS.Model;
using System.Collections.ObjectModel;
using System.Windows.Automation;
using System.Windows.Input;
namespace SHBMS.ViewModel;
public class InvoiceViewModel : DatabaseViewModel
{
    public ObservableCollection<InvoiceDto> Invoices { get; set; } = new ObservableCollection<InvoiceDto>();

    private InvoiceDto _selectedInvoice;
    public InvoiceDto SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            _selectedInvoice = value;
            OnPropertyChanged(nameof(SelectedInvoice));

            // Create a selectedItem to copy data to form. 
            if (_selectedInvoice != null)
            {
                selectedItem = new InvoiceDto
                {
                    InvoiceId = _selectedInvoice.InvoiceId,
                    BookingId = _selectedInvoice.BookingId,
                    CustomerName = _selectedInvoice.CustomerName,
                    TotalAmount = _selectedInvoice.TotalAmount,
                    InvoiceDate = _selectedInvoice.InvoiceDate,
                    CheckinDate = _selectedInvoice.CheckinDate,
                    CheckoutDate = _selectedInvoice.CheckoutDate,
                    Rooms = _selectedInvoice.Rooms,
                    Services = _selectedInvoice.Services
                };
            }
        }
    }

    // Binding to detail form 
    private InvoiceDto _selectedItem;
    public InvoiceDto selectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged(nameof(selectedItem));
        }
    }

    // Find key
    private string _searchtext;
    public string searchtext
    {
        get => _searchtext;
        set
        {
            _searchtext = value;
            OnPropertyChanged(nameof(searchtext));
        }
    }

    public ICommand RefreshCmd { get; }
    public ICommand SearchCmd { get; }

    public InvoiceViewModel()
    {
        selectedItem = new InvoiceDto();

        SearchCmd = DatabaseCommand(LoadDataAsync);
        // Refresh command clears the search text and reloads data
        RefreshCmd = DatabaseCommand(async () => { searchtext = ""; OnPropertyChanged(nameof(searchtext)); await LoadDataAsync(); });
    }

    protected override async Task LoadDataAsync()
    {
        using (var _context = new Prn212SbhmsContext())
        {
            var query = _context.Invoices
                .Include(i => i.Booking).ThenInclude(b => b.Customer)
                .Include(i => i.Booking).ThenInclude(b => b.BookingDetails).ThenInclude(bd => bd.RoomType)
                .Include(i => i.Booking).ThenInclude(b => b.BookingServices).ThenInclude(bs => bs.Service)
                .AsQueryable();

            // Filter by search text if provided
            if (!string.IsNullOrWhiteSpace(searchtext))
            {
                query = query.Where(i => i.Booking.Customer.FullName.Contains(searchtext));
            }

            // Convert the query to a list and format the data for display
            var rawData = await query.ToListAsync();

            var formattedData = rawData.Select(i => new InvoiceDto
            {
                InvoiceId = i.InvoiceId,
                BookingId = i.BookingId,
                StayId = i.BookingId,
                CustomerName = i.Booking.Customer.FullName,
                EmployeeId = i.EmployeeId,
                InvoiceDate = i.InvoiceDate,
                CheckinDate = i.Booking.CheckInDate,
                CheckoutDate = i.Booking.CheckOutDate,
                TotalAmount = i.TotalAmount.ToString("N0"),
               
                Rooms = i.Booking.BookingDetails.Any()
                    ? string.Join(", ", i.Booking.BookingDetails.Select(d => d.RoomType.TypeName))
                    : "No Room",

                Services = i.Booking.BookingServices.Any()
                    ? string.Join(", ", i.Booking.BookingServices.Select(s => s.Service.ServiceName))
                    : "No Service"
            }).ToList();

            Invoices.Clear();
            foreach (var item in formattedData)
            {
                Invoices.Add(item);
            }
        }
    }
}