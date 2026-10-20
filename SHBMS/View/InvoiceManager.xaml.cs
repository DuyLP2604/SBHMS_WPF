using SBHMS.DTO;
using SHBMS.DTO; 
using SHBMS.DTO.Invoice; 
using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace SHBMS.View 
{
    public partial class InvoiceManager : UserControl
    {
        private readonly ObservableCollection<InvoiceDto> _invoices = new();

        public InvoiceManager()
        {
            InitializeComponent();

            LoadSampleData();
            InvoiceGrid.ItemsSource = _invoices;
        }

        // Add sample data for test
        private void LoadSampleData()
        {
            _invoices.Add(new InvoiceDto
            {
                InvoiceId = 1001,
                StayId = 501,
                CustomerName = "Trương Gia Hân",
                RoomNumber = "A101",
                TotalAmount = 450.00m,
                CreatedDate = DateTime.Now.AddDays(-1),
                CheckInDate = DateTime.Now.AddDays(-4),
                CheckOutDate = DateTime.Now.AddDays(-1),
                ServiceOrders = new System.Collections.Generic.List<ServiceOrderDto>
                {
                    new ServiceOrderDto { ServiceName = "Giặt ủi", Quantity = 1, UnitPrice = 20m },
                    new ServiceOrderDto { ServiceName = "Spa", Quantity = 1, UnitPrice = 100m }
                }
            });

            _invoices.Add(new InvoiceDto
            {
                InvoiceId = 1002,
                StayId = 502,
                CustomerName = "Nguyen Minh Anh",
                RoomNumber = "B202",
                TotalAmount = 120.50m,
                CreatedDate = DateTime.Now.AddHours(-5),
                CheckInDate = DateTime.Now.AddDays(-2),
                CheckOutDate = DateTime.Now,
                ServiceOrders = new System.Collections.Generic.List<ServiceOrderDto>
                {
                    new ServiceOrderDto { ServiceName = "Nước suối", Quantity = 2, UnitPrice = 2.5m }
                }
            });

            _invoices.Add(new InvoiceDto
            {
                InvoiceId = 1003,
                StayId = 503,
                CustomerName = "Tran Gia Bao",
                RoomNumber = "C303",
                TotalAmount = 800.00m,
                CreatedDate = DateTime.Now.AddMinutes(-30),
                CheckInDate = DateTime.Now.AddDays(-5),
                CheckOutDate = DateTime.Now,
                ServiceOrders = new System.Collections.Generic.List<ServiceOrderDto>()
            });
        }

        // Selection invoice to display details each invoice
        private void InvoiceGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InvoiceGrid.SelectedItem is InvoiceDto selectedInvoice)
            {
                txtBookingId.Text = selectedInvoice.StayId.ToString();
                txtInvoiceId.Text = selectedInvoice.InvoiceId.ToString();
                txtCreatedDate.Text = selectedInvoice.CreatedDate.ToString("dd/MM/yyyy HH:mm");
                txtCustomerName.Text = selectedInvoice.CustomerName;
                txtRoomNumber.Text = selectedInvoice.RoomNumber;

                // 4. Xóa dấu trừ (-) dư thừa và format lại chuỗi hiển thị ngày tháng trên 1 dòng
                txtStayPeriod.Text = $"{selectedInvoice.CheckInDate:dd/MM/yyyy} - {selectedInvoice.CheckOutDate:dd/MM/yyyy}";

                txtServices.Text = selectedInvoice.DisplayService;
                txtTotalAmount.Text = selectedInvoice.TotalAmount.ToString("C");
            }
            else
            {
                ClearDetails();
            }
        }

        private void ClearDetails()
        {
            txtBookingId.Text = "--";
            txtInvoiceId.Text = "--";
            txtCreatedDate.Text = "--";
            txtCustomerName.Text = "--";
            txtRoomNumber.Text = "--";
            txtStayPeriod.Text = "--";
            txtServices.Text = "--";
            txtTotalAmount.Text = "$0.00";
        }
    }
}