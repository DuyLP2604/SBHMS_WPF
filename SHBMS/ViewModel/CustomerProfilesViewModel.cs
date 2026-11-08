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

// Manages customer profiles, form validation, and list filtering.
public class CustomerProfilesViewModel : DatabaseViewModel
{
    // Stores customer rows shown in the list.
    public ObservableCollection<Customer> Customers { get; } = new();
    // Provides nationality options for the form.
    public ObservableCollection<Nationality> Nationalities { get; } = new();
    // Provides a filtered view of the customer collection.
    public ICollectionView CustomersView { get; }

    private Customer? _selectedCus;
    // Copies the selected row into the editor without changing the list row.
    public Customer? SelectedCus
    {
        get => _selectedCus;
        set
        {
            if (!SetProperty(ref _selectedCus, value)) return;
            if (value != null) SelectedItem = CopyCustomer(value);
            // Requests a refresh of command availability.
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // Starts with an empty form; the customer ID is assigned during creation.
    private Customer _selectedItem = EmptyCustomer();
    // Holds editable form values separately from the selected row.
    public Customer SelectedItem
    {
        get => _selectedItem;
        private set => SetProperty(ref _selectedItem, value);
    }

    private string _searchText = "";
    // Stores the keyword applied when the list view is refreshed.
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    // Creates a customer using the current form values.
    public RelayCommand NewCommand { get; }
    // Updates the selected customer using the form values.
    public RelayCommand SaveCommand { get; }
    // Deletes the selected customer when no linked records exist.
    public RelayCommand DeleteCommand { get; }
    // Applies the search keyword to the customer list.
    public RelayCommand SearchCommand { get; }
    // Clears the keyword and shows all customers.
    public RelayCommand ShowAllCommand { get; }

    // Sets up the customer filter and UI commands.
    public CustomerProfilesViewModel()
    {
        CustomersView = CollectionViewSource.GetDefaultView(Customers);
        // Matches the keyword against customer details and nationality.
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

    // Returns blank form values with a temporary ID label.
    private static Customer EmptyCustomer() => new()
    {
        CustomerId = "(auto)", FullName = "", Phone = "",
        IdentityNumber = "", Email = "", Address = ""
    };

    // Copies editable fields and replaces null text with empty strings.
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

    // Clears the row selection and resets the editor.
    private void ClearForm()
    {
        SelectedCus = null;
        SelectedItem = EmptyCustomer();
    }

    // Reloads customer rows and nationality options from the database.
    protected override async Task LoadDataAsync()
    {
        using var db = new Prn212SbhmsContext();
        // Display data does not need EF change tracking.
        var nations = await db.Nationalities.AsNoTracking()
            .OrderBy(n => n.NationalityName).ToListAsync();
        var customers = await db.Customers.AsNoTracking()
            .Include(c => c.Nationality).OrderBy(c => c.CustomerId).ToListAsync();

        Nationalities.Clear();
        // Adds a blank option so nationality can be left unselected.
        Nationalities.Add(new Nationality { NationalityId = "", NationalityName = "" });
        foreach (var nation in nations) Nationalities.Add(nation);
        Customers.Clear();
        foreach (var customer in customers) Customers.Add(customer);
        ClearForm();
    }

    // Checks required values, field lengths, and email format.
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
        // Keeps text within the database column limits.
        if (form.FullName.Trim().Length > 100 || form.Phone.Trim().Length > 15
            || (form.IdentityNumber?.Trim().Length ?? 0) > 20
            || (form.Email?.Trim().Length ?? 0) > 100
            || (form.Address?.Trim().Length ?? 0) > 200)
        {
            Notify("One or more fields exceed the allowed length.", "warning");
            return false;
        }
        // Validates email only when a value is provided.
        if (!string.IsNullOrWhiteSpace(form.Email)
            && (!MailAddress.TryCreate(form.Email.Trim(), out var email)
                || !email.Address.Equals(form.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            Notify("Invalid email address.", "warning");
            return false;
        }
        return true;
    }

    // Checks duplicate contact details and the selected nationality.
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

        // Compares optional email addresses without case differences.
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

        // Checks identity number uniqueness only when it is provided.
        string? identity = EntityIds.Optional(form.IdentityNumber);
        if (identity != null && await db.Customers.AnyAsync(c =>
            c.IdentityNumber == identity && c.CustomerId != excludeId))
        {
            Notify("Identity number already exists. Enter a different identity number.", "warning");
            return false;
        }
        // Ensures a selected nationality still exists in the database.
        string? nationalityId = EntityIds.Optional(form.NationalityId);
        if (nationalityId != null && !await db.Nationalities.AnyAsync(n => n.NationalityId == nationalityId))
        {
            Notify("Select a nationality from the list.", "warning");
            return false;
        }
        return true;
    }

    // Trims required text and normalizes optional values before saving.
    private static void ApplyForm(Customer form, Customer entity)
    {
        entity.FullName = form.FullName.Trim();
        entity.Phone = form.Phone.Trim();
        entity.IdentityNumber = EntityIds.Optional(form.IdentityNumber);
        entity.Email = EntityIds.Optional(form.Email);
        entity.Address = EntityIds.Optional(form.Address);
        entity.NationalityId = EntityIds.Optional(form.NationalityId);
    }

    // Validates the form and inserts a new customer with a generated ID.
    private async Task CreateAsync()
    {
        // New creates a separate customer, even when an existing row is selected.
        var form = CopyCustomer(SelectedItem);
        if (!ValidateForm(form)) return;
        using var db = new Prn212SbhmsContext();
        if (!await ValidateDatabaseAsync(db, form)) return;

        // Builds a customer ID using the CUS prefix and three-digit padding.
        var customer = new Customer
        {
            CustomerId = EntityIds.Next(
                await db.Customers.Select(c => c.CustomerId).ToListAsync(), "CUS", 3)
        };
        ApplyForm(form, customer);
        db.Customers.Add(customer);
        if (!await SaveCustomerChangesAsync(db)) return;

        // Reloads the list and selects the newly created customer.
        string newId = customer.CustomerId;
        SearchText = "";
        await LoadDataAsync();
        SelectedCus = Customers.FirstOrDefault(c => c.CustomerId == newId);
        Notify($"Customer {newId} created successfully.", "success");
    }

    // Validates and saves changes to the selected customer.
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
        // Excludes this customer from duplicate checks during an update.
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

    // Saves changes and shows friendly messages for duplicate database keys.
    private async Task<bool> SaveCustomerChangesAsync(Prn212SbhmsContext db)
    {
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        // SQL Server codes 2601 and 2627 indicate duplicate key values.
        catch (DbUpdateException ex) when (ex.GetBaseException() is SqlException sql
            && (sql.Number == 2601 || sql.Number == 2627))
        {
            // A concurrent save can cause duplicates after validation has passed.
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

    // Refreshes the filtered list and reports when no rows match.
    private void Search()
    {
        CustomersView.Refresh();
        if (CustomersView.IsEmpty) Notify("No customers found.", "warning");
    }

    // Confirms deletion and protects customers with linked records.
    private async Task DeleteAsync()
    {
        if (SelectedCus == null)
        {
            Notify("Please select a customer to delete.", "warning");
            return;
        }
        string id = SelectedCus.CustomerId;
        // Requires user confirmation before deleting the selected customer.
        if (MessageBox.Show($"Delete customer {id}?", "Delete customer",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        using var db = new Prn212SbhmsContext();
        // Keeps customer records that are used by bookings or feedback.
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
