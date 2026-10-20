using SHBMS.Helper;
using SHBMS.Model;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;


namespace SHBMS.View
{


    public partial class StaffAccountView : UserControl
    {
        private readonly ObservableCollection<StaffAccountModel> _accounts = new();
        private StaffAccountModel? _selectedAccount;

        public StaffAccountView()
        {
            InitializeComponent();
            LoadSampleData();
            StaffGrid.ItemsSource = _accounts;
            PrepareNewForm();
        }

        private void LoadSampleData()
        {
            _accounts.Add(new StaffAccountModel { Username = "admin01", FullName = "Nguyễn Văn Anh", Role = "Admin", Phone = "0901234567", Email = "admin@hotel.com", IsActive = true });
            _accounts.Add(new StaffAccountModel { Username = "mgr_binh", FullName = "Trần Thị Bình", Role = "Manager", Phone = "0912345678", Email = "binh@hotel.com", IsActive = true });
            _accounts.Add(new StaffAccountModel { Username = "staff_cuong", FullName = "Lê Quốc Cường", Role = "Staff", Phone = "0923456789", Email = "cuong@hotel.com", IsActive = false });
        }

        private void PrepareNewForm()
        {
            _selectedAccount = null;
            StaffGrid.SelectedItem = null;
            UsernameBox.IsReadOnly = false;
            UsernameBox.Clear();
            FullNameBox.Clear();
            RoleCombo.SelectedIndex = 2; // Mặc định là Staff
            PhoneBox.Clear();
            EmailBox.Clear();
            PasswordBox.Clear();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            PrepareNewForm();
            ToastService.Show("Sẵn sàng tạo tài khoản nhân viên mới.", "success");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string fullName = FullNameBox.Text.Trim();
            string phone = PhoneBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
            {
                ToastService.Show("Vui lòng điền Username, Full Name và Phone.", "warning");
                return;
            }

            string role = (RoleCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Staff";

            if (_selectedAccount == null) // Thêm mới
            {
                if (_accounts.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    ToastService.Show("Username này đã tồn tại!", "error");
                    return;
                }

                var newAcc = new StaffAccountModel
                {
                    Username = username,
                    FullName = fullName,
                    Role = role,
                    Phone = phone,
                    Email = EmailBox.Text.Trim(),
                    Password = PasswordBox.Text.Trim(),
                    IsActive = true
                };
                _accounts.Add(newAcc);
                ToastService.Show("Thêm tài khoản nhân viên thành công!", "success");
            }
            else // Cập nhật
            {
                _selectedAccount.FullName = fullName;
                _selectedAccount.Role = role;
                _selectedAccount.Phone = phone;
                _selectedAccount.Email = EmailBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(PasswordBox.Text))
                    _selectedAccount.Password = PasswordBox.Text.Trim();

                StaffGrid.Items.Refresh();
                ToastService.Show("Cập nhật thông tin tài khoản thành công!", "success");
            }

            PrepareNewForm();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string kw = SearchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(kw))
            {
                StaffGrid.ItemsSource = _accounts;
                return;
            }

            StaffGrid.ItemsSource = _accounts.Where(a =>
                a.Username.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                a.FullName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                a.Role.Contains(kw, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            StaffGrid.ItemsSource = _accounts;
            SearchBox.Clear();
        }

        private void ToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            if (StaffGrid.SelectedItem is StaffAccountModel acc)
            {
                acc.IsActive = !acc.IsActive;
                StaffGrid.Items.Refresh();
                ToastService.Show($"Đã {(acc.IsActive ? "mở khóa" : "khóa")} tài khoản {acc.Username}.", "success");
            }
            else
            {
                ToastService.Show("Vui lòng chọn 1 tài khoản để khóa/mở khóa.", "warning");
            }
        }

        private void StaffGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StaffGrid.SelectedItem is StaffAccountModel acc)
            {
                _selectedAccount = acc;
                UsernameBox.Text = acc.Username;
                UsernameBox.IsReadOnly = true; // Không cho sửa Username khi sửa thông tin
                FullNameBox.Text = acc.FullName;
                PhoneBox.Text = acc.Phone;
                EmailBox.Text = acc.Email;
                PasswordBox.Clear();

                for (int i = 0; i < RoleCombo.Items.Count; i++)
                {
                    if ((RoleCombo.Items[i] as ComboBoxItem)?.Content?.ToString() == acc.Role)
                    {
                        RoleCombo.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
    }
}