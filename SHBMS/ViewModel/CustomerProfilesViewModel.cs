using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SHBMS.Helper;
using SHBMS.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Mail;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SHBMS.ViewModel;

public class CustomerProfilesViewModel : DatabaseViewModel
{
    public ObservableCollection<Customer> Customers { get; } = new();
    public ObservableCollection<Nationality> Nationalities { get; } = new();
    public ICollectionView CustomersView { get; }

    // SelectedCus is the original row. SelectedItem is the editable copy.
    private Customer? _selectedCus;
    public Customer? SelectedCus
    {
        get => _selectedCus;
        set
        {
            if (!SetProperty(ref _selectedCus, value)) return;
            if (value != null) SelectedItem = CopyCustomer(value);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private Customer _selectedItem = EmptyCustomer();
    public Customer SelectedItem
    {
        get => _selectedItem;
        private set => SetProperty(ref _selectedItem, value);
    }

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public RelayCommand NewCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ShowAllCommand { get; }

    public CustomerProfilesViewModel()
    {
        CustomersView = CollectionViewSource.GetDefaultView(Customers);
        CustomersView.Filter = item => item is Customer c &&
            (string.IsNullOrWhiteSpace(SearchText)
             || Contains(c.CustomerId, SearchText.Trim())
             || Contains(c.FullName, SearchText.Trim())
             || Contains(c.IdentityNumber, SearchText.Trim())
             || Contains(c.Phone, SearchText.Trim())
             || Contains(c.Email, SearchText.Trim())
             || Contains(c.Address, SearchText.Trim())
             || Contains(c.NationalityName, SearchText.Trim()));

        NewCommand = DatabaseCommand(CreateAsync);
        SaveCommand = DatabaseCommand(UpdateAsync);
        DeleteCommand = DatabaseCommand(DeleteAsync);
        SearchCommand = new RelayCommand(_ => Search(), _ => !IsBusy);
        ShowAllCommand = new RelayCommand(_ =>
        {
            SearchText = "";
            CustomersView.Refresh();
        }, _ => !IsBusy);
    }

    private static Customer EmptyCustomer() => new()
    {
        CustomerId = "(auto)", FullName = "", Phone = "",
        IdentityNumber = "", Email = "", Address = ""
    };

    private static Customer CopyCustomer(Customer source) => new()
    {
        CustomerId = source.CustomerId,
        FullName = source.FullName ?? "",
        IdentityNumber = source.IdentityNumber ?? "",
        Phone = source.Phone ?? "",
        Email = source.Email ?? "",
        Address = source.Address ?? "",
        NationalityId = source.NationalityId
    };

    private void ClearForm()
    {
        SelectedCus = null;
        SelectedItem = EmptyCustomer();
    }

    protected override async Task LoadDataAsync()
    {
        using var db = new Prn212SbhmsContext();
        var nations = await db.Nationalities.AsNoTracking()
            .OrderBy(n => n.NationalityName).ToListAsync();
        var customers = await db.Customers.AsNoTracking()
            .Include(c => c.Nationality).OrderBy(c => c.CustomerId).ToListAsync();

        Nationalities.Clear();
        Nationalities.Add(new Nationality { NationalityId = "", NationalityName = "" });
        foreach (var nation in nations) Nationalities.Add(nation);
        Customers.Clear();
        foreach (var customer in customers) Customers.Add(customer);
        ClearForm();
    }

    private bool ValidateForm(Customer form)
    {
        if (string.IsNullOrWhiteSpace(form.FullName))
        {
            Notify("Full name is required.", "warning");
            return false;
        }
        if (string.IsNullOrWhiteSpace(form.Phone))
        {
            Notify("Phone number is required.", "warning");
            return false;
        }
        if (form.FullName.Trim().Length > 100 || form.Phone.Trim().Length > 15
            || (form.IdentityNumber?.Trim().Length ?? 0) > 20
            || (form.Email?.Trim().Length ?? 0) > 100
            || (form.Address?.Trim().Length ?? 0) > 200)
        {
            Notify("One or more fields exceed the allowed length.", "warning");
            return false;
        }
        if (!string.IsNullOrWhiteSpace(form.Email)
            && (!MailAddress.TryCreate(form.Email.Trim(), out var email)
                || !email.Address.Equals(form.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            Notify("Invalid email address.", "warning");
            return false;
        }
        return true;
    }

    private async Task<bool> ValidateDatabaseAsync(
        Prn212SbhmsContext db, Customer form, string? excludeId = null)
    {
        // Check all customers in the database, excluding the row being updated.
        string phone = form.Phone.Trim();
        if (await db.Customers.AnyAsync(c =>
            c.CustomerId != excludeId && c.Phone.Trim() == phone))
        {
            Notify("Phone number already exists. Enter a different phone number.", "warning");
            return false;
        }

        string? email = EntityIds.Optional(form.Email);
        if (email != null)
        {
            string normalizedEmail = email.ToLower();
            if (await db.Customers.AnyAsync(c =>
                c.CustomerId != excludeId && c.Email != null
                && c.Email.Trim().ToLower() == normalizedEmail))
            {
                Notify("Email already exists. Enter a different email address.", "warning");
                return false;
            }
        }

        string? identity = EntityIds.Optional(form.IdentityNumber);
        if (identity != null && await db.Customers.AnyAsync(c =>
            c.IdentityNumber == identity && c.CustomerId != excludeId))
        {
            Notify("Identity number already exists. Enter a different identity number.", "warning");
            return false;
        }
        string? nationalityId = EntityIds.Optional(form.NationalityId);
        if (nationalityId != null && !await db.Nationalities.AnyAsync(n => n.NationalityId == nationalityId))
        {
            Notify("Select a nationality from the list.", "warning");
            return false;
        }
        return true;
    }

    private static void ApplyForm(Customer form, Customer entity)
    {
        entity.FullName = form.FullName.Trim();
        entity.Phone = form.Phone.Trim();
        entity.IdentityNumber = EntityIds.Optional(form.IdentityNumber);
        entity.Email = EntityIds.Optional(form.Email);
        entity.Address = EntityIds.Optional(form.Address);
        entity.NationalityId = EntityIds.Optional(form.NationalityId);
    }

    private async Task CreateAsync()
    {
        // New creates a different customer from the form; it never updates SelectedCus.
        var form = CopyCustomer(SelectedItem);
        if (!ValidateForm(form)) return;
        using var db = new Prn212SbhmsContext();
        if (!await ValidateDatabaseAsync(db, form)) return;

        var customer = new Customer
        {
            CustomerId = EntityIds.Next(
                await db.Customers.Select(c => c.CustomerId).ToListAsync(), "CUS", 3)
        };
        ApplyForm(form, customer);
        db.Customers.Add(customer);
        if (!await SaveCustomerChangesAsync(db)) return;

        string newId = customer.CustomerId;
        SearchText = "";
        await LoadDataAsync();
        SelectedCus = Customers.FirstOrDefault(c => c.CustomerId == newId);
        Notify($"Customer {newId} created successfully.", "success");
    }

    private async Task UpdateAsync()
    {
        if (SelectedCus == null)
        {
            Notify("Select a customer to update. Use New customer to create one from the form.", "warning");
            return;
        }
        string id = SelectedCus.CustomerId;
        var form = CopyCustomer(SelectedItem);
        if (!ValidateForm(form)) return;
        using var db = new Prn212SbhmsContext();
        if (!await ValidateDatabaseAsync(db, form, id)) return;
        var customer = await db.Customers.FindAsync(id);
        if (customer == null)
        {
            Notify("Customer no longer exists. Refresh the list.", "warning");
            return;
        }
        ApplyForm(form, customer);
        if (!await SaveCustomerChangesAsync(db)) return;
        SearchText = "";
        await LoadDataAsync();
        Notify("Customer updated successfully.", "success");
    }

    private async Task<bool> SaveCustomerChangesAsync(Prn212SbhmsContext db)
    {
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is SqlException sql
            && (sql.Number == 2601 || sql.Number == 2627))
        {
            // The database is still authoritative if another user saves concurrently.
            string message = ex.GetBaseException().Message;
            if (message.Contains("UX_CUSTOMER_Identity_NotNull", StringComparison.OrdinalIgnoreCase))
                Notify("Identity number already exists. Enter a different identity number.", "warning");
            else if (message.Contains("PK_CUSTOMER", StringComparison.OrdinalIgnoreCase))
                Notify("Customer ID already exists. Please try creating the customer again.", "warning");
            else
                Notify("Customer information conflicts with an existing record. Refresh and try again.", "warning");
            return false;
        }
    }

    private void Search()
    {
        CustomersView.Refresh();
        if (CustomersView.IsEmpty) Notify("No customers found.", "warning");
    }

    private async Task DeleteAsync()
    {
        if (SelectedCus == null)
        {
            Notify("Please select a customer to delete.", "warning");
            return;
        }
        string id = SelectedCus.CustomerId;
        if (MessageBox.Show($"Delete customer {id}?", "Delete customer",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        using var db = new Prn212SbhmsContext();
        if (await db.Bookings.AnyAsync(b => b.CustomerId == id)
            || await db.Complaints.AnyAsync(c => c.CustomerId == id))
        {
            Notify("This customer has linked bookings or feedback and cannot be deleted.", "warning");
            return;
        }
        var customer = await db.Customers.FindAsync(id);
        if (customer == null)
        {
            Notify("Customer no longer exists. Refresh the list.", "warning");
            return;
        }
        db.Customers.Remove(customer);
        await db.SaveChangesAsync();
        SearchText = "";
        await LoadDataAsync();
        Notify("Customer deleted successfully.", "success");
    }
}