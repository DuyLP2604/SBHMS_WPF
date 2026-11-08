using Microsoft.EntityFrameworkCore;
using SHBMS.Helper;
using SHBMS.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Mail;
using System.Windows;
using System.Windows.Data;
namespace SHBMS.ViewModel;
public class CustomerProfilesViewModel : DatabaseViewModel
{
    public ObservableCollection<Customer> Customers { get; } = new();
    public ObservableCollection<string> Nationalities { get; } = new();
    public ICollectionView CustomersView { get; }
    private string _customerId = "";
    public string CustomerId { get => _customerId; set => SetProperty(ref _customerId, value); }
    private string _fullName = "";
    public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
    private string _identityNumber = "";
    public string IdentityNumber { get => _identityNumber; set => SetProperty(ref _identityNumber, value); }
    private string _phone = "";
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    private string _email = "";
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    private string _address = "";
    public string Address { get => _address; set => SetProperty(ref _address, value); }
    private string _nationalityName = "";
    public string NationalityName { get => _nationalityName; set => SetProperty(ref _nationalityName, value ?? ""); }
    private string _searchText = "";
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    private Customer? _selectedCustomer;
    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            SetProperty(ref _selectedCustomer, value);
            CustomerId = value?.CustomerId ?? "(auto)";
            FullName = value?.FullName ?? ""; IdentityNumber = value?.IdentityNumber ?? "";
            Phone = value?.Phone ?? ""; Email = value?.Email ?? "";
            Address = value?.Address ?? ""; NationalityName = value?.NationalityName ?? "";
        }
    }
    public RelayCommand NewCommand { get; }
    public RelayCommand CopyCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ShowAllCommand { get; }
    public CustomerProfilesViewModel()
    {
        CustomersView = CollectionViewSource.GetDefaultView(Customers);
        CustomersView.Filter = item => item is Customer c && (string.IsNullOrWhiteSpace(SearchText)
            || Contains(c.CustomerId, SearchText.Trim()) || Contains(c.FullName, SearchText.Trim())
            || Contains(c.Phone, SearchText.Trim()) || Contains(c.Email, SearchText.Trim())
            || Contains(c.IdentityNumber, SearchText.Trim()));
        NewCommand = new RelayCommand(_ => SelectedCustomer = null, _ => !IsBusy);
        CopyCommand = new RelayCommand(_ =>
        {
            string name = FullName, phone = Phone, email = Email, address = Address, nation = NationalityName;
            SelectedCustomer = null;
            FullName = name; Phone = phone; Email = email; Address = address; NationalityName = nation;
        }, _ => !IsBusy && SelectedCustomer != null);
        SaveCommand = DatabaseCommand(SaveAsync);
        DeleteCommand = DatabaseCommand(DeleteAsync, () => SelectedCustomer != null);
        SearchCommand = new RelayCommand(_ => CustomersView.Refresh(), _ => !IsBusy);
        ShowAllCommand = new RelayCommand(_ => { SearchText = ""; CustomersView.Refresh(); }, _ => !IsBusy);
        SelectedCustomer = null;
    }
    protected override async Task LoadDataAsync()
    {
        using var db = new Prn212SbhmsContext();
        var nations = await db.Nationalities.AsNoTracking().OrderBy(n => n.NationalityName)
            .Select(n => n.NationalityName).ToListAsync();
        Nationalities.Clear();
        Nationalities.Add(""); // Nationality is optional on a customer profile.
        foreach (var nation in nations) Nationalities.Add(nation);
        var data = await db.Customers.AsNoTracking().Include(c => c.Nationality)
            .OrderBy(c => c.CustomerId).ToListAsync();
        Customers.Clear(); foreach (var c in data) Customers.Add(c);
        SelectedCustomer = null;
    }
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Phone))
        { StatusMessage = "Full name and phone are required."; return; }
        if (FullName.Trim().Length > 100 || Phone.Trim().Length > 15 || IdentityNumber.Trim().Length > 20
            || Email.Trim().Length > 100 || Address.Trim().Length > 200 || NationalityName.Trim().Length > 50)
        { StatusMessage = "A field exceeds its allowed length."; return; }
        if (!string.IsNullOrWhiteSpace(Email) && (!MailAddress.TryCreate(Email.Trim(), out var mail) || mail.Address != Email.Trim()))
        { StatusMessage = "Invalid email address."; return; }
        using var db = new Prn212SbhmsContext();
        string? id = SelectedCustomer?.CustomerId;
        string? identity = EntityIds.Optional(IdentityNumber);
        if (identity != null && await db.Customers.AnyAsync(c => c.IdentityNumber == identity && c.CustomerId != id))
        { StatusMessage = "Identity number already exists."; return; }
        var entity = id == null ? new Customer
        {
            CustomerId = EntityIds.Next(await db.Customers.Select(c => c.CustomerId).ToListAsync(), "CUS", 3)
        } : await db.Customers.FindAsync(id);
        if (entity == null) { StatusMessage = "Customer no longer exists. Refresh the list."; return; }
        entity.FullName = FullName.Trim(); entity.Phone = Phone.Trim(); entity.IdentityNumber = identity;
        entity.Email = EntityIds.Optional(Email); entity.Address = EntityIds.Optional(Address);
        string? nationName = EntityIds.Optional(NationalityName);
        var nationality = nationName == null ? null : await db.Nationalities
            .SingleOrDefaultAsync(n => n.NationalityName == nationName);
        if (nationName != null && nationality == null)
        { StatusMessage = "Select a nationality from the list."; return; }
        entity.NationalityId = nationality?.NationalityId;
        if (id == null) db.Customers.Add(entity);
        await db.SaveChangesAsync();
        await LoadDataAsync();
        StatusMessage = "Customer saved to database.";
    }
    private async Task DeleteAsync()
    {
        if (SelectedCustomer == null) return;
        string id = SelectedCustomer.CustomerId;
        if (MessageBox.Show($"Delete customer {id}?", "Delete customer", MessageBoxButton.YesNo,
            MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        using var db = new Prn212SbhmsContext();
        if (await db.Bookings.AnyAsync(b => b.CustomerId == id) || await db.Complaints.AnyAsync(c => c.CustomerId == id))
        { StatusMessage = "This customer has linked bookings or feedback and cannot be deleted."; return; }
        var entity = await db.Customers.FindAsync(id);
        if (entity != null) { db.Customers.Remove(entity); await db.SaveChangesAsync(); }
        await LoadDataAsync(); StatusMessage = "Customer deleted.";
    }
}
