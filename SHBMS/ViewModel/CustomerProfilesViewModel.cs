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

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    // SelectedItem: row selected in the grid.
    // TextBoxItem: a separate Customer copy used by the input form.
    private Customer? _selectedItem;
    public Customer? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value)) return;

            if (value != null)
            {
                TextBoxItem = CloneCustomer(value);
                StatusMessage = "";
            }

            // Clearing the grid selection does not change the form.
            // New explicitly clears the form below.
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private Customer _textBoxItem = EmptyCustomer();
    public Customer TextBoxItem
    {
        get => _textBoxItem;
        private set
        {
            SetProperty(ref _textBoxItem, value);
            OnPropertyChanged(nameof(IsNew));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // New customers have no ID until Save generates one.
    public bool IsNew => string.IsNullOrEmpty(TextBoxItem.CustomerId);

    public RelayCommand NewCommand { get; }
    public RelayCommand CopyCommand { get; }
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
             || Contains(c.Phone, SearchText.Trim())
             || Contains(c.Email, SearchText.Trim())
             || Contains(c.IdentityNumber, SearchText.Trim()));

        NewCommand = new RelayCommand(_ => PrepareNewForm(), _ => !IsBusy);
        CopyCommand = new RelayCommand(_ => CopyToNew(),
            _ => !IsBusy && !IsNew);
        SaveCommand = DatabaseCommand(SaveAsync);
        DeleteCommand = DatabaseCommand(DeleteAsync, () => SelectedItem != null);
        SearchCommand = new RelayCommand(_ => CustomersView.Refresh(), _ => !IsBusy);
        ShowAllCommand = new RelayCommand(_ =>
        {
            SearchText = "";
            CustomersView.Refresh();
        }, _ => !IsBusy);
    }

    private static Customer EmptyCustomer() => new()
    {
        CustomerId = "",
        FullName = "",
        IdentityNumber = "",
        Phone = "",
        Email = "",
        Address = "",
        NationalityId = null
    };

    // Copy only scalar fields; do not copy EF navigation collections.
    private static Customer CloneCustomer(Customer source) => new()
    {
        CustomerId = source.CustomerId,
        FullName = source.FullName ?? "",
        IdentityNumber = source.IdentityNumber ?? "",
        Phone = source.Phone ?? "",
        Email = source.Email ?? "",
        Address = source.Address ?? "",
        NationalityId = source.NationalityId
    };

    private void PrepareNewForm()
    {
        SelectedItem = null;
        TextBoxItem = EmptyCustomer();
        StatusMessage = "";
        OnPropertyChanged(nameof(IsNew));
        CommandManager.InvalidateRequerySuggested();
    }

    private void CopyToNew()
    {
        // Preserve edits already entered on the form, without saving them.
        var copy = CloneCustomer(TextBoxItem);
        PrepareNewForm();
        copy.CustomerId = "";
        copy.IdentityNumber = "";
        TextBoxItem = copy;
        Notify("Customer details copied. Enter a new identity number if needed, then save.", "info");
    }

    protected override async Task LoadDataAsync()
    {
        using var db = new Prn212SbhmsContext();
        var nations = await db.Nationalities.AsNoTracking()
            .OrderBy(n => n.NationalityName).ToListAsync();
        var data = await db.Customers.AsNoTracking()
            .Include(c => c.Nationality)
            .OrderBy(c => c.CustomerId).ToListAsync();

        Nationalities.Clear();
        Nationalities.Add(new Nationality
        {
            NationalityId = "",
            NationalityName = ""
        }); // Empty option: nationality is optional.
        foreach (var nation in nations) Nationalities.Add(nation);

        Customers.Clear();
        foreach (var customer in data) Customers.Add(customer);
        PrepareNewForm();
    }

    private async Task SaveAsync()
    {
        var form = TextBoxItem;
        bool isNew = string.IsNullOrEmpty(form.CustomerId);
        string? id = isNew ? null : form.CustomerId;

        if (string.IsNullOrWhiteSpace(form.FullName) ||
            string.IsNullOrWhiteSpace(form.Phone))
        {
            Notify("Full name and phone are required.", "warning");
            return;
        }

        if (form.FullName.Trim().Length > 100 || form.Phone.Trim().Length > 15
            || (form.IdentityNumber?.Trim().Length ?? 0) > 20
            || (form.Email?.Trim().Length ?? 0) > 100
            || (form.Address?.Trim().Length ?? 0) > 200)
        {
            Notify("A field exceeds its allowed length.", "warning");
            return;
        }

        if (!string.IsNullOrWhiteSpace(form.Email) &&
            (!MailAddress.TryCreate(form.Email.Trim(), out var mail)
             || mail.Address != form.Email.Trim()))
        {
            Notify("Invalid email address.", "warning");
            return;
        }

        using var db = new Prn212SbhmsContext();
        string? identity = EntityIds.Optional(form.IdentityNumber);
        if (identity != null && await db.Customers.AnyAsync(c =>
            c.IdentityNumber == identity && c.CustomerId != id))
        {
            Notify("Identity number already exists.", "warning");
            return;
        }

        Customer entity;
        if (isNew)
        {
            entity = new Customer
            {
                CustomerId = EntityIds.Next(
                    await db.Customers.Select(c => c.CustomerId).ToListAsync(), "CUS", 3)
            };
        }
        else
        {
            var existingCustomer = await db.Customers.FindAsync(id);
            if (existingCustomer == null)
            {
                Notify("Customer no longer exists. Refresh the list.", "warning");
                return;
            }
            entity = existingCustomer;
        }

        string? nationalityId = EntityIds.Optional(form.NationalityId);
        if (nationalityId != null && !await db.Nationalities
            .AnyAsync(n => n.NationalityId == nationalityId))
        {
            Notify("Select a nationality from the list.", "warning");
            return;
        }

        entity.FullName = form.FullName.Trim();
        entity.Phone = form.Phone.Trim();
        entity.IdentityNumber = identity;
        entity.Email = EntityIds.Optional(form.Email);
        entity.Address = EntityIds.Optional(form.Address);
        entity.NationalityId = nationalityId;

        if (isNew) db.Customers.Add(entity);
        await db.SaveChangesAsync();
        await LoadDataAsync();
        Notify(isNew ? "Customer added successfully." : "Customer updated successfully.", "success");
    }

    private async Task DeleteAsync()
    {
        if (SelectedItem == null) return;
        string id = SelectedItem.CustomerId;
        if (MessageBox.Show($"Delete customer {id}?", "Delete customer",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        using var db = new Prn212SbhmsContext();
        if (await db.Bookings.AnyAsync(b => b.CustomerId == id)
            || await db.Complaints.AnyAsync(c => c.CustomerId == id))
        {
            Notify("This customer has linked bookings or feedback and cannot be deleted.", "warning");
            return;
        }

        var entity = await db.Customers.FindAsync(id);
        if (entity == null)
        {
            Notify("Customer no longer exists. Refresh the list.", "warning");
            return;
        }

        db.Customers.Remove(entity);
        await db.SaveChangesAsync();
        await LoadDataAsync();
        Notify("Customer deleted.", "success");
    }
}
