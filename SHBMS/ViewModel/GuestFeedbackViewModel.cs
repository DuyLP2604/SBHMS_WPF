using Microsoft.EntityFrameworkCore;
using SHBMS.Helper;
using SHBMS.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
namespace SHBMS.ViewModel;
public class GuestFeedbackViewModel : DatabaseViewModel
{
    public ObservableCollection<Complaint> Feedback { get; } = new();
    public ICollectionView FeedbackView { get; }
    private string _replyText = "";
    public string ReplyText { get => _replyText; set => SetProperty(ref _replyText, value); }
    private string _searchText = "";
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    public string Summary => $"Total: {Feedback.Count} | Resolved: {Feedback.Count(c => c.Status == "Đã xử lý")}";
    private Complaint? _selectedFeedback;
    public Complaint? SelectedFeedback
    {
        get => _selectedFeedback;
        set { SetProperty(ref _selectedFeedback, value); ReplyText = value?.ReplyMessage ?? ""; }
    }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ShowAllCommand { get; }
    public RelayCommand PendingCommand { get; }
    public RelayCommand ResolveCommand { get; }
    public GuestFeedbackViewModel()
    {
        FeedbackView = CollectionViewSource.GetDefaultView(Feedback);
        FeedbackView.Filter = item => item is Complaint c && (string.IsNullOrWhiteSpace(SearchText)
            || Contains(c.Title, SearchText.Trim()) || Contains(c.Content, SearchText.Trim())
            || Contains(c.Customer?.FullName, SearchText.Trim()) || Contains(c.Status, SearchText.Trim()));
        SearchCommand = new RelayCommand(_ => FeedbackView.Refresh(), _ => !IsBusy);
        ShowAllCommand = new RelayCommand(_ => { SearchText = ""; FeedbackView.Refresh(); }, _ => !IsBusy);
        PendingCommand = DatabaseCommand(() => SaveStatusAsync("Chưa xử lý"), () => SelectedFeedback != null);
        ResolveCommand = DatabaseCommand(() => SaveStatusAsync("Đã xử lý"), () => SelectedFeedback != null);
    }
    protected override async Task LoadDataAsync()
    {
        using var db = new Prn212SbhmsContext();
        var data = await db.Complaints.AsNoTracking().Include(c => c.Customer).OrderByDescending(c => c.CreatedAt).ToListAsync();
        Feedback.Clear(); foreach (var c in data) Feedback.Add(c);
        SelectedFeedback = null; OnPropertyChanged(nameof(Summary));
    }
    private async Task SaveStatusAsync(string status)
    {
        if (SelectedFeedback == null) return;
        string? employeeId = Session.CurrentAccount?.EmployeeId;
        if (employeeId == null) { StatusMessage = "This account has no linked employee."; return; }
        if (status == "Đã xử lý" && string.IsNullOrWhiteSpace(ReplyText))
        { StatusMessage = "Enter a reply before resolving feedback."; return; }
        using var db = new Prn212SbhmsContext();
        var entity = await db.Complaints.FindAsync(SelectedFeedback.ComplaintId);
        if (entity == null) { StatusMessage = "Feedback no longer exists. Refresh the list."; return; }
        entity.Status = status; entity.ReplyMessage = EntityIds.Optional(ReplyText);
        entity.ResolvedByEmployee = status == "Đã xử lý" ? employeeId : null;
        await db.SaveChangesAsync(); await LoadDataAsync(); StatusMessage = "Feedback saved to database.";
    }
}
