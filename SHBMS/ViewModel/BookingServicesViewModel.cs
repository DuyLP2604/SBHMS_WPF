using Microsoft.EntityFrameworkCore;
using SHBMS.Helper;
using SHBMS.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SHBMS.ViewModel
{
    public class BookingServicesViewModel : BaseViewModel
    {
        private const string CheckedIn = "CHECKED_IN";

        public ObservableCollection<SHBMS.Model.BookingService> Orders { get; } = new();

        public ObservableCollection<Booking> Bookings { get; } = new();

        public ObservableCollection<SHBMS.Model.Service> Services { get; } = new();

        private SHBMS.Model.BookingService? _selectedOrder;
        public SHBMS.Model.BookingService? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                _selectedOrder = value;
                OnPropertyChanged();

                SelectedBooking = Bookings.FirstOrDefault(b => b.BookingId == value?.BookingId);
                SelectedService = Services.FirstOrDefault(s => s.ServiceId == value?.ServiceId);
                QuantityText = value == null ? "1" : value.Quantity.ToString();

                OnPropertyChanged(nameof(IsNew));
            }
        }

        private Booking? _selectedBooking;
        public Booking? SelectedBooking
        {
            get => _selectedBooking;
            set
            {
                _selectedBooking = value;
                OnPropertyChanged();
            }
        }

        private SHBMS.Model.Service? _selectedService;
        public SHBMS.Model.Service? SelectedService
        {
            get => _selectedService;
            set
            {
                _selectedService = value;
                OnPropertyChanged();
            }
        }
        private string _quantityText = "1";
        public string QuantityText
        {
            get => _quantityText;
            set
            {
                _quantityText = value;
                OnPropertyChanged();
            }
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
        public bool IsNew => SelectedOrder == null;

        public RelayCommand NewCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public BookingServicesViewModel()
        {
            NewCommand = new RelayCommand(p => New());
            SaveCommand = new RelayCommand(p => Save());
            DeleteCommand = new RelayCommand(p => Delete(), p => SelectedOrder != null);
            RefreshCommand = new RelayCommand(p => LoadData());
        }

        private static string GetError(Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;

            return ex.Message;
        }
        public Task ReloadAsync()
        {
            LoadData();
            return Task.CompletedTask;
        }

        public void LoadData()
        {
            try
            {
                using var db = new Prn212SbhmsContext();

                var bookings = db.Bookings
                    .AsNoTracking()
                    .Where(b => !b.IsDeleted && b.BookingStatus == CheckedIn)
                    .OrderBy(b => b.BookingId)
                    .ToList();

                var services = db.Services
                    .AsNoTracking()
                    .OrderBy(s => s.ServiceName)
                    .ToList();

                var orders = db.BookingServices
                    .AsNoTracking()
                    .Include(o => o.Service)
                    .Where(o => !o.Booking.IsDeleted && o.Booking.BookingStatus == CheckedIn)
                    .OrderByDescending(o => o.BookingId)
                    .ThenByDescending(o => o.ServiceId)
                    .ToList();

                Bookings.Clear();
                foreach (var booking in bookings)
                    Bookings.Add(booking);

                Services.Clear();
                foreach (var service in services)
                    Services.Add(service);

                Orders.Clear();
                foreach (var order in orders)
                    Orders.Add(order);

                SelectedOrder = null;
            }
            catch (Exception ex)
            {
                StatusMessage = "Cannot load data: " + GetError(ex);
            }
        }

        private void New()
        {
            try
            {
                using var db = new Prn212SbhmsContext();

                var bookings = db.Bookings
                    .AsNoTracking()
                    .Where(b => !b.IsDeleted && b.BookingStatus == CheckedIn)
                    .OrderBy(b => b.BookingId)
                    .ToList();

                Bookings.Clear();
                foreach (var booking in bookings)
                    Bookings.Add(booking);

                SelectedOrder = null;

                StatusMessage = bookings.Count == 0
                    ? "No booking is checked in right now."
                    : "Pick a checked-in booking, a service and a quantity, then click Save.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Cannot load bookings: " + GetError(ex);
            }
        }

        private void Save()
        {
            if (!int.TryParse(QuantityText, out int quantity) || quantity < 1)
            {
                StatusMessage = "Quantity must be a whole number greater than 0.";
                return;
            }

            string message;

            try
            {
                using var db = new Prn212SbhmsContext();

                if (IsNew)
                {
                    if (SelectedBooking == null || SelectedService == null)
                    {
                        StatusMessage = "Please select a checked-in booking and a service.";
                        return;
                    }
                    var booking = db.Bookings.Find(SelectedBooking.BookingId);

                    if (booking == null || booking.IsDeleted || booking.BookingStatus != CheckedIn)
                    {
                        StatusMessage = "This booking is no longer checked in. Click Refresh.";
                        return;
                    }
                    var existing = db.BookingServices.Find(
                        SelectedBooking.BookingId,
                        SelectedService.ServiceId);

                    if (existing != null)
                    {
                        StatusMessage = "This service is already ordered. Select its row to change the quantity.";
                        return;
                    }

                    db.BookingServices.Add(new SHBMS.Model.BookingService
                    {
                        BookingId = SelectedBooking.BookingId,
                        ServiceId = SelectedService.ServiceId,
                        Quantity = quantity,
                        UnitPrice = SelectedService.UnitPrice,
                        OrderedByEmployee = booking.EmployeeId
                    });

                    message = "Service order added.";
                }
                else
                {
                    var order = db.BookingServices
                        .Include(o => o.Booking)
                        .FirstOrDefault(o =>
                            o.BookingId == SelectedOrder!.BookingId &&
                            o.ServiceId == SelectedOrder.ServiceId);

                    if (order == null)
                    {
                        StatusMessage = "Order no longer exists. Click Refresh.";
                        return;
                    }

                    if (order.Booking.IsDeleted || order.Booking.BookingStatus != CheckedIn)
                    {
                        StatusMessage = "Service orders can only be edited for checked-in bookings.";
                        return;
                    }

                    order.Quantity = quantity;
                    message = "Quantity updated.";
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                StatusMessage = "Cannot save: " + GetError(ex);
                MessageBox.Show(GetError(ex), "Cannot save", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadData();
            StatusMessage = message;
        }
        private void Delete()
        {
            if (SelectedOrder == null)
                return;

            var result = MessageBox.Show(
                "Remove the selected service order?",
                "Remove service",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using var db = new Prn212SbhmsContext();

                var order = db.BookingServices
                    .Include(o => o.Booking)
                    .FirstOrDefault(o =>
                        o.BookingId == SelectedOrder.BookingId &&
                        o.ServiceId == SelectedOrder.ServiceId);

                if (order == null)
                {
                    StatusMessage = "Order no longer exists. Click Refresh.";
                    return;
                }

                if (order.Booking.IsDeleted || order.Booking.BookingStatus != CheckedIn)
                {
                    StatusMessage = "Service orders can only be removed for checked-in bookings.";
                    return;
                }

                db.BookingServices.Remove(order);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                StatusMessage = "Cannot remove: " + GetError(ex);
                return;
            }

            LoadData();
            StatusMessage = "Service order removed.";
        }
    }
}