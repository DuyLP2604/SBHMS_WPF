
using SHBMS.Helper;
using SHBMS.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;

namespace SHBMS.View
{
    public partial class CustomerProfilesView : UserControl
    {
        // Demo data - no database
        private readonly ObservableCollection<CustomerProfile>
            _customers = new();

        private int _nextId = 4;

        // Current selected customer
        private CustomerProfile? _editingCustomer;

        // Temporarily stored customer
        private CustomerProfile? _temporaryCustomer;

        public CustomerProfilesView()
        {
            InitializeComponent();

            LoadSampleData();

            CustomerGrid.ItemsSource = _customers;

            PrepareNewForm();
        }

        // ==========================================
        // SAMPLE DATA
        // ==========================================
        private void LoadSampleData()
        {
            _customers.Add(new CustomerProfile
            {
                CustomerID = "CUS001",
                FullName = "Nguyen Minh Anh",
                IdentityNumber = "079201001234",
                Phone = "0901234567",
                Email = "minhanh@example.com",
                Address = "Can Tho, Vietnam",
                Nationality = "Vietnamese"
            });

            _customers.Add(new CustomerProfile
            {
                CustomerID = "CUS002",
                FullName = "Tran Gia Bao",
                IdentityNumber = "079202005678",
                Phone = "0912345678",
                Email = "giabao@example.com",
                Address = "Ho Chi Minh City, Vietnam",
                Nationality = "Vietnamese"
            });

            _customers.Add(new CustomerProfile
            {
                CustomerID = "CUS003",
                FullName = "Emily Johnson",
                IdentityNumber = "P12345678",
                Phone = "0987654321",
                Email = "emily@example.com",
                Address = "London, United Kingdom",
                Nationality = "British"
            });
        }

        // ==========================================
        // GET NEXT CUSTOMER ID
        // ==========================================
        private string GetNextCustomerId()
        {
            int next = _nextId;
            string id;

            do
            {
                id = $"CUS{next++:000}";
            }
            while (_customers.Any(c =>
                c.CustomerID.Equals(
                    id,
                    StringComparison.OrdinalIgnoreCase)));

            return id;
        }

        // ==========================================
        // GENERATE CUSTOMER ID
        // ==========================================
        private string GenerateCustomerId()
        {
            string id;

            do
            {
                id = $"CUS{_nextId++:000}";
            }
            while (_customers.Any(c =>
                c.CustomerID.Equals(
                    id,
                    StringComparison.OrdinalIgnoreCase)));

            return id;
        }

        // ==========================================
        // COPY CUSTOMER DATA
        // ==========================================
        private CustomerProfile CopyCustomer(
            CustomerProfile source)
        {
            return new CustomerProfile
            {
                CustomerID = source.CustomerID,
                FullName = source.FullName,
                IdentityNumber = source.IdentityNumber,
                Phone = source.Phone,
                Email = source.Email,
                Address = source.Address,
                Nationality = source.Nationality
            };
        }

        // ==========================================
        // READ FORM
        // ==========================================
        private CustomerProfile ReadForm()
        {
            return new CustomerProfile
            {
                CustomerID = CustomerIdBox.Text.Trim(),
                FullName = FullNameBox.Text.Trim(),
                IdentityNumber = IdentityNumberBox.Text.Trim(),
                Phone = PhoneBox.Text.Trim(),
                Email = EmailBox.Text.Trim(),
                Address = AddressBox.Text.Trim(),
                Nationality = NationalityBox.Text.Trim()
            };
        }

        // ==========================================
        // DISPLAY CUSTOMER
        // ==========================================
        private void DisplayCustomer(CustomerProfile customer)
        {
            CustomerIdBox.Text = customer.CustomerID;
            FullNameBox.Text = customer.FullName;
            IdentityNumberBox.Text = customer.IdentityNumber;
            PhoneBox.Text = customer.Phone;
            EmailBox.Text = customer.Email;
            AddressBox.Text = customer.Address;
            NationalityBox.Text = customer.Nationality;
        }

        // ==========================================
        // PREPARE EMPTY FORM
        // ==========================================
        private void PrepareNewForm()
        {
            _editingCustomer = null;
            _temporaryCustomer = null;

            CustomerGrid.SelectedItem = null;

            CustomerIdBox.Text = GetNextCustomerId();
            FullNameBox.Clear();
            IdentityNumberBox.Clear();
            PhoneBox.Clear();
            EmailBox.Clear();
            AddressBox.Clear();
            NationalityBox.Clear();
        }

        // ==========================================
        // VALIDATE CUSTOMER
        // ==========================================
        private bool ValidateCustomer(
            CustomerProfile customer,
            string? excludeCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(customer.FullName))
            {
                ToastService.Show(
                    "Full name is required.",
                    "warning");
                FullNameBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                ToastService.Show(
                    "Phone number is required.",
                    "warning");
                PhoneBox.Focus();
                return false;
            }

            if (customer.FullName.Length > 100 ||
                customer.IdentityNumber.Length > 20 ||
                customer.Phone.Length > 15 ||
                customer.Email.Length > 100 ||
                customer.Address.Length > 200 ||
                customer.Nationality.Length > 50)
            {
                ToastService.Show(
                    "One or more fields exceed the allowed length.",
                    "warning");
                return false;
            }

            // Check email format
            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                try
                {
                    var email = new MailAddress(customer.Email);

                    if (!email.Address.Equals(
                        customer.Email,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        throw new FormatException();
                    }
                }
                catch (FormatException)
                {
                    ToastService.Show(
                        "Invalid email address.",
                        "warning");
                    EmailBox.Focus();
                    return false;
                }
            }

            // Identity Number must be unique
            if (!string.IsNullOrWhiteSpace(
                customer.IdentityNumber))
            {
                bool exists = _customers.Any(c =>
                    c.IdentityNumber.Equals(
                        customer.IdentityNumber,
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    !c.CustomerID.Equals(
                        excludeCustomerId,
                        StringComparison.OrdinalIgnoreCase));

                if (exists)
                {
                    ToastService.Show(
                        "Identity number already exists.",
                        "warning");
                    IdentityNumberBox.Focus();
                    return false;
                }
            }

            return true;
        }

        // ==========================================
        // NEW CUSTOMER
        // ==========================================
        private void New_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Copy current form if editing a customer
            if (_editingCustomer != null)
            {
                _temporaryCustomer = ReadForm();

                // Copy information to a new customer
                var newCustomer =
                    CopyCustomer(_temporaryCustomer);

                newCustomer.CustomerID =
                    GetNextCustomerId();

                // Identity Number must be unique.
                // A different customer cannot reuse it.
                newCustomer.IdentityNumber = "";

                if (!ValidateCustomer(newCustomer))
                    return;

                newCustomer.CustomerID =
                    GenerateCustomerId();

                _customers.Add(newCustomer);

                // Restore full list
                CustomerGrid.ItemsSource = _customers;
                SearchBox.Clear();

                CustomerGrid.SelectedItem = newCustomer;
                CustomerGrid.ScrollIntoView(newCustomer);

                _editingCustomer = newCustomer;

                DisplayCustomer(newCustomer);

                _temporaryCustomer = null;

                ToastService.Show(
                    $"Customer {newCustomer.CustomerID} created. " +
                    "Please enter a new identity number if needed.",
                    "success");
            }
            else
            {
                PrepareNewForm();

                ToastService.Show(
                    "Ready to create a new customer.",
                    "success");
            }
        }

        // ==========================================
        // SAVE CUSTOMER
        // ==========================================
        private void Save_Click(
            object sender,
            RoutedEventArgs e)
        {
            CustomerProfile formData = ReadForm();

            string? excludeId =
                _editingCustomer?.CustomerID;

            if (!ValidateCustomer(formData, excludeId))
                return;

            // ADD
            if (_editingCustomer == null)
            {
                var newCustomer = new CustomerProfile
                {
                    CustomerID = GenerateCustomerId(),
                    FullName = formData.FullName,
                    IdentityNumber = formData.IdentityNumber,
                    Phone = formData.Phone,
                    Email = formData.Email,
                    Address = formData.Address,
                    Nationality = formData.Nationality
                };

                _customers.Add(newCustomer);

                ToastService.Show(
                    "Customer added successfully!",
                    "success");
            }
            // UPDATE
            else
            {
                _editingCustomer.FullName =
                    formData.FullName;

                _editingCustomer.IdentityNumber =
                    formData.IdentityNumber;

                _editingCustomer.Phone =
                    formData.Phone;

                _editingCustomer.Email =
                    formData.Email;

                _editingCustomer.Address =
                    formData.Address;

                _editingCustomer.Nationality =
                    formData.Nationality;

                ToastService.Show(
                    "Customer updated successfully!",
                    "success");
            }

            CustomerGrid.ItemsSource = _customers;
            CustomerGrid.Items.Refresh();

            SearchBox.Clear();

            PrepareNewForm();
        }

        // ==========================================
        // SEARCH
        // ==========================================
        private void Search_Click(
            object sender,
            RoutedEventArgs e)
        {
            string keyword = SearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                CustomerGrid.ItemsSource = _customers;
                return;
            }

            var results = _customers.Where(c =>
                c.CustomerID.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) ||

                c.FullName.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) ||

                c.IdentityNumber.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) ||

                c.Phone.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) ||

                c.Email.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) ||

                c.Address.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) ||

                c.Nationality.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase)
            ).ToList();

            CustomerGrid.ItemsSource = results;

            if (results.Count == 0)
            {
                ToastService.Show(
                    "No customers found.",
                    "warning");
            }
        }

        // ==========================================
        // SHOW ALL
        // ==========================================
        private void ShowAll_Click(
            object sender,
            RoutedEventArgs e)
        {
            CustomerGrid.ItemsSource = _customers;
            SearchBox.Clear();
        }

        // ==========================================
        // DELETE
        // ==========================================
        private void Delete_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (CustomerGrid.SelectedItem
                is not CustomerProfile customer)
            {
                ToastService.Show(
                    "Please select a customer to delete.",
                    "warning");
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete " +
                $"{customer.FullName}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            _customers.Remove(customer);

            CustomerGrid.ItemsSource = _customers;
            SearchBox.Clear();

            PrepareNewForm();

            ToastService.Show(
                "Customer deleted successfully!",
                "success");
        }

        // ==========================================
        // SELECT CUSTOMER
        // ==========================================
        private void CustomerGrid_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (CustomerGrid.SelectedItem
                is CustomerProfile customer)
            {
                _editingCustomer = customer;

                // Store temporary customer information
                _temporaryCustomer = CopyCustomer(customer);

                // Show all 7 fields
                DisplayCustomer(customer);
            }
        }
    }
}
