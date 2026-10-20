
using SHBMS.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SHBMS.View
{
    public partial class ServiceRequestsView : UserControl
    {
        // Danh sách yêu cầu lưu tạm trong bộ nhớ
        private readonly ObservableCollection<ServiceRequestItem> _requests = new();
        private ServiceRequestItem? _selectedRequest;

        // Khởi tạo giao diện FE-08
        public ServiceRequestsView()
        {
            InitializeComponent();
            ApplyFilter();
        }

        // Lấy giá trị ComboBox
        private string GetComboText(ComboBox combo)
        {
            return (combo.SelectedItem as ComboBoxItem)?
                .Content?.ToString() ?? "";
        }

        // Cập nhật thống kê theo toàn bộ danh sách
        private void UpdateCount()
        {
            CountText.Text = $"Total: {_requests.Count} | " +
                $"Pending: {_requests.Count(x => x.Status == "Pending")} | " +
                $"In Progress: {_requests.Count(x => x.Status == "InProgress")} | " +
                $"Resolved: {_requests.Count(x => x.Status == "Resolved")}";
        }

        // Lọc dữ liệu theo tab, trạng thái và từ khóa
        private void ApplyFilter()
        {
            if (RequestsGrid == null ||
                SearchBox == null ||
                RequestTabs == null ||
                StatusFilterBox == null)
                return;

            string keyword = SearchBox.Text.Trim();
            string status = GetComboText(StatusFilterBox);

            string type = RequestTabs.SelectedIndex == 1
                ? "Complaint"
                : "Service Request";

            var result = _requests.Where(r =>
                r.RequestType == type &&
                (string.IsNullOrWhiteSpace(keyword) ||
                 r.RequestId.ToString().Contains(keyword) ||
                 r.RoomNumber.Contains(
                     keyword,
                     StringComparison.OrdinalIgnoreCase) ||
                 r.RequestDetails.Contains(
                     keyword,
                     StringComparison.OrdinalIgnoreCase)) &&
                (status == "All Status" || r.Status == status)
            ).ToList();

            RequestsGrid.ItemsSource = result;
            UpdateCount();
        }

        // Chuyển tab giữa yêu cầu dịch vụ và khiếu nại
        private void RequestTabs_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, RequestTabs))
                return;

            if (RequestTypeBox != null)
                RequestTypeBox.SelectedIndex =
                    RequestTabs.SelectedIndex == 1 ? 1 : 0;

            ApplyFilter();
        }

        // Hiển thị chi tiết yêu cầu được chọn
        private void RequestChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (RequestsGrid.SelectedItem
                is not ServiceRequestItem request)
            {
                _selectedRequest = null;

                if (SelectedRequestText != null)
                    SelectedRequestText.Text =
                        "No request selected";

                return;
            }

            _selectedRequest = request;

            RoomBox.Text = request.RoomNumber;
            DetailsBox.Text = request.RequestDetails;
            NotesBox.Text = request.Notes;

            SelectedRequestText.Text =
                $"Request #{request.RequestId} - Room {request.RoomNumber}";

            foreach (ComboBoxItem item in RequestTypeBox.Items)
            {
                if (item.Content?.ToString() ==
                    request.RequestType)
                {
                    RequestTypeBox.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in StatusBox.Items)
            {
                if (item.Content?.ToString() ==
                    request.Status)
                {
                    StatusBox.SelectedItem = item;
                    break;
                }
            }
        }

        // Làm mới bảng dữ liệu
        private void RefreshGrid()
        {
            ApplyFilter();
        }

        // Tạo form yêu cầu mới theo tab đang mở
        private void New_Click(
            object sender,
            RoutedEventArgs e)
        {
            RequestsGrid.SelectedItem = null;
            _selectedRequest = null;

            RoomBox.Clear();
            DetailsBox.Clear();
            NotesBox.Clear();

            RequestTypeBox.SelectedIndex =
                RequestTabs.SelectedIndex == 1 ? 1 : 0;

            StatusBox.SelectedIndex = 0;
            SelectedRequestText.Text = "New request";

            RoomBox.Focus();
        }

        // Thêm mới hoặc lưu nội dung yêu cầu
        private void Save_Click(
            object sender,
            RoutedEventArgs e)
        {
            string room = RoomBox.Text.Trim();
            string details = DetailsBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(room) ||
                string.IsNullOrWhiteSpace(details))
            {
                MessageBox.Show(
                    "Please enter room and request details.");
                return;
            }

            string type = GetComboText(RequestTypeBox);

            if (_selectedRequest == null)
            {
                int newId = _requests.Count == 0
                    ? 1
                    : _requests.Max(x => x.RequestId) + 1;

                _requests.Add(new ServiceRequestItem
                {
                    RequestId = newId,
                    RoomNumber = room,
                    RequestType = type,
                    RequestDetails = details,
                    Notes = NotesBox.Text.Trim(),
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                });

                MessageBox.Show(
                    "New request created successfully!");
            }
            else
            {
                _selectedRequest.RoomNumber = room;
                _selectedRequest.RequestType = type;
                _selectedRequest.RequestDetails = details;
                _selectedRequest.Notes = NotesBox.Text.Trim();

                MessageBox.Show(
                    "Request saved successfully!");
            }

            // Hiển thị tab đúng loại yêu cầu vừa lưu
            RequestTabs.SelectedIndex =
                type == "Complaint" ? 1 : 0;

            RefreshGrid();
        }

        // Kiểm tra yêu cầu đang chọn
        private ServiceRequestItem? GetSelectedRequest()
        {
            if (_selectedRequest == null)
            {
                MessageBox.Show(
                    "Please select a request first.");
            }

            return _selectedRequest;
        }

        // Tiếp nhận yêu cầu Pending
        private void Accept_Click(
            object sender,
            RoutedEventArgs e)
        {
            var request = GetSelectedRequest();

            if (request == null)
                return;

            if (request.Status != "Pending")
            {
                MessageBox.Show(
                    "Only pending requests can be accepted.");
                return;
            }

            request.Status = "InProgress";
            request.Notes = NotesBox.Text.Trim();

            StatusBox.SelectedIndex = 1;

            RefreshGrid();

            MessageBox.Show(
                "Request accepted successfully!");
        }

        // Cập nhật trạng thái và thông tin yêu cầu
        private void Update_Click(
            object sender,
            RoutedEventArgs e)
        {
            var request = GetSelectedRequest();

            if (request == null)
                return;

            if (string.IsNullOrWhiteSpace(RoomBox.Text) ||
                string.IsNullOrWhiteSpace(DetailsBox.Text))
            {
                MessageBox.Show(
                    "Room and details cannot be empty.");
                return;
            }

            request.RoomNumber = RoomBox.Text.Trim();
            request.RequestType = GetComboText(RequestTypeBox);
            request.RequestDetails = DetailsBox.Text.Trim();
            request.Notes = NotesBox.Text.Trim();
            request.Status = GetComboText(StatusBox);

            RequestTabs.SelectedIndex =
                request.RequestType == "Complaint" ? 1 : 0;

            RefreshGrid();

            MessageBox.Show(
                "Request updated successfully!");
        }

        // Đánh dấu yêu cầu đã giải quyết
        private void Resolve_Click(
            object sender,
            RoutedEventArgs e)
        {
            var request = GetSelectedRequest();

            if (request == null)
                return;

            if (request.Status == "Rejected")
            {
                MessageBox.Show(
                    "Rejected requests cannot be resolved.");
                return;
            }

            request.Status = "Resolved";
            request.Notes = NotesBox.Text.Trim();

            StatusBox.SelectedIndex = 2;

            RefreshGrid();

            MessageBox.Show(
                "Request resolved successfully!");
        }

        // Tìm kiếm yêu cầu
        private void Search_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyFilter();
        }

        // Lọc theo trạng thái
        private void FilterChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        // Xóa bộ lọc trong tab hiện tại
        private void ShowAll_Click(
            object sender,
            RoutedEventArgs e)
        {
            SearchBox.Clear();
            StatusFilterBox.SelectedIndex = 0;

            ApplyFilter();
        }
    }
}
