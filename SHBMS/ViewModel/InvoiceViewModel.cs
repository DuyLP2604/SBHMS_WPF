using Microsoft.EntityFrameworkCore;
using SHBMS.Model;
using System.Collections.ObjectModel;
namespace SHBMS.ViewModel;
public class InvoiceViewModel : DatabaseViewModel
{
    public ObservableCollection<Invoice> Invoices { get; } = new();
    private Invoice? _selectedInvoice;
    public Invoice? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            SetProperty(ref _selectedInvoice, value);
            OnPropertyChanged(nameof(RoomText)); OnPropertyChanged(nameof(ServiceText)); OnPropertyChanged(nameof(StayPeriod));
        }
    }
    public string RoomText => SelectedInvoice == null ? "" : string.Join(", ", SelectedInvoice.Booking.BookingDetails
        .SelectMany(d => d.RoomAssignments).Select(a => a.Room.RoomNumber));
    public string ServiceText => SelectedInvoice == null ? "" : string.Join(", ", SelectedInvoice.Booking.BookingServices
        .Select(s => $"{s.Service.ServiceName} x {s.Quantity}"));
    public string StayPeriod => SelectedInvoice == null ? "" : $"{SelectedInvoice.Booking.CheckInDate:dd/MM/yyyy} - {SelectedInvoice.Booking.CheckOutDate:dd/MM/yyyy}";
    protected override async Task LoadDataAsync()
    {
        using var db = new Prn212SbhmsContext();
        var data = await db.Invoices.AsNoTracking().Include(i => i.Booking).ThenInclude(b => b.Customer)
            .Include(i => i.Booking).ThenInclude(b => b.BookingDetails).ThenInclude(d => d.RoomAssignments).ThenInclude(a => a.Room)
            .Include(i => i.Booking).ThenInclude(b => b.BookingServices).ThenInclude(s => s.Service)
            .OrderByDescending(i => i.InvoiceDate).ToListAsync();
        Invoices.Clear(); foreach (var invoice in data) Invoices.Add(invoice); SelectedInvoice = null;
    }
}
