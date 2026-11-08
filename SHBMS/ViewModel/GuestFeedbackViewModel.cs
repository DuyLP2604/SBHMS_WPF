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
    public string Summary => $"Total: {Feedback.Count} | Resolved: {Feedback.Count(f => f.Status == "Đã xử lý")}";

    private Complaint? _selectedFeedback;
    public Complaint? SelectedFeedback
    {
        get => _selectedFeedback;
        set
        {
            if (SetProperty(ref _selectedFeedback, value))
                ReplyText = value?.ReplyMessage ?? "";
        }
    }

    // Editing a reply does not change the selected row until Resolve is used.
    private string _replyText = "";
    public string ReplyText
    {
        get => _replyText;
        set => SetProperty(ref _replyText, value);
    }
    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public RelayCommand SearchCommand { get; }
    public RelayCommand ShowAllCommand { get; }
    public RelayCommand PendingCommand { get; }
    public RelayCommand ResolveCommand { get; }

    public GuestFeedbackViewModel()
    {
        FeedbackView = CollectionViewSource.GetDefaultView(Feedback);
        FeedbackView.Filter = item => item is Complaint f &&
            (string.IsNullOrWhiteSpace(SearchText)
             || Contains(f.CustomerId, SearchText.Trim())
             || Contains(f.Customer?.FullName, SearchText.Trim())
             || Contains(f.Title, SearchText.Trim())
             || Contains(f.Content, SearchText.Trim())
             || Contains(f.Status, SearchText.Trim()));
        SearchCommand = new RelayCommand(_ => FeedbackView.Refresh(), _ => !IsBusy);
        ShowAllCommand = new RelayCommand(_ =>
        {
            SearchText = "";
            FeedbackView.Refresh();
        }, _ => !IsBusy);
        PendingCommand = DatabaseCommand(() => SaveStatusAsync(false));
        ResolveCommand = DatabaseCommand(() => SaveStatusAsync(true));
    }

    protected override async Task LoadDataAsync()
    {
        using var db = new Prn212SbhmsContext();
        var data = await db.Complaints.AsNoTracking().Include(f => f.Customer)
            .OrderByDescending(f => f.CreatedAt).ToListAsync();
        Feedback.Clear();
        foreach (var feedback in data) Feedback.Add(feedback);
        SelectedFeedback = null;
        ReplyText = "";
        OnPropertyChanged(nameof(Summary));
    }

    private async Task SaveStatusAsync(bool resolve)
    {
        if (SelectedFeedback == null)
        {
            Notify("Please select a feedback first.", "warning");
            return;
        }
        int id = SelectedFeedback.ComplaintId;
        string reply = ReplyText;
        if (resolve && string.IsNullOrWhiteSpace(reply))
        {
            Notify("Please enter a reply before resolving.", "warning");
            return;
        }
        string? employeeId = Session.CurrentAccount?.EmployeeId;
        if (resolve && employeeId == null)
        {
            Notify("This account has no linked employee.", "warning");
            return;
        }
        using var db = new Prn212SbhmsContext();
        var feedback = await db.Complaints.FindAsync(id);
        if (feedback == null)
        {
            Notify("Feedback no longer exists. Refresh the list.", "warning");
            return;
        }
        if (resolve)
        {
            feedback.Status = "Resolved";
            feedback.ReplyMessage = reply.Trim();
            feedback.ResolvedByEmployee = employeeId;
        }
        else
        {
            feedback.Status = "Pending";
            feedback.ResolvedByEmployee = null;
        }
        await db.SaveChangesAsync();
        await LoadDataAsync();
        SelectedFeedback = Feedback.FirstOrDefault(f => f.ComplaintId == id);
        if (!resolve) ReplyText = reply;
        Notify(resolve ? "Feedback resolved successfully!" : "Feedback marked as pending.", "success");
    }
}
