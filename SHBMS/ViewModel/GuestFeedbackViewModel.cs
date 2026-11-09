using Microsoft.EntityFrameworkCore;
using SHBMS.Helper;
using SHBMS.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace SHBMS.ViewModel;

// Loads, filters, and updates guest feedback.
public class GuestFeedbackViewModel : DatabaseViewModel
{
    // Checks whether the current user can change replies and statuses.
    public bool CanResolve => Session.CanResolveGuestFeedback;

    // Prevents reply editing when the user has no resolve permission.
    public bool IsReplyReadOnly => !CanResolve;

    // Stores feedback rows shown in the list.
    public ObservableCollection<Complaint> Feedback { get; } = new();

    // Provides a filtered view of the feedback collection.
    public ICollectionView FeedbackView { get; }

    // Counts all feedback and rows with the Vietnamese resolved status.
    public string Summary => $"Total: {Feedback.Count} | Resolved: {Feedback.Count(f => f.Status == "Resolved")}";

    private Complaint? _selectedFeedback;

    // Loads the saved reply when the selected feedback changes.
    public Complaint? SelectedFeedback
    {
        get => _selectedFeedback;
        set
        {
            if (SetProperty(ref _selectedFeedback, value))
                ReplyText = value?.ReplyMessage ?? "";
        }
    }

    private string _replyText = "";

    // Keeps reply edits separate from the selected row until Resolve is used.
    public string ReplyText
    {
        get => _replyText;
        set => SetProperty(ref _replyText, value);
    }

    private string _searchText = "";

    // Stores the keyword used when the feedback view is refreshed.
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    // Applies the current search keyword.
    public RelayCommand SearchCommand { get; }

    // Clears the keyword and shows all feedback.
    public RelayCommand ShowAllCommand { get; }

    // Marks the selected feedback as pending.
    public RelayCommand PendingCommand { get; }

    // Saves the reply and marks the selected feedback as resolved.
    public RelayCommand ResolveCommand { get; }

    // Sets up the feedback filter and UI commands.
    public GuestFeedbackViewModel()
    {
        FeedbackView = CollectionViewSource.GetDefaultView(Feedback);

        // Matches the keyword against customer details and feedback fields.
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

        // Both status commands require permission to resolve feedback.
        PendingCommand = DatabaseCommand(() => SaveStatusAsync(false), () => CanResolve);
        ResolveCommand = DatabaseCommand(() => SaveStatusAsync(true), () => CanResolve);
    }

    // Loads feedback with customer details, newest first.
    protected override async Task LoadDataAsync()
    {
        // Checks view permission before reading the database.
        if (!Session.CanViewGuestFeedback)
            throw new UnauthorizedAccessException("Only Staff and Manager can view guest feedback.");

        using var db = new Prn212SbhmsContext();

        // These display rows do not need EF change tracking.
        var data = await db.Complaints.AsNoTracking().Include(f => f.Customer)
            .OrderByDescending(f => f.CreatedAt).ToListAsync();

        Feedback.Clear();
        foreach (var feedback in data) Feedback.Add(feedback);

        // Clears the editor and updates the summary after reloading.
        SelectedFeedback = null;
        ReplyText = "";
        OnPropertyChanged(nameof(Summary));
    }

    // Saves a resolved or pending status for the selected feedback.
    private async Task SaveStatusAsync(bool resolve)
    {
        // Managers can review feedback; only Staff can change it.
        if (!CanResolve)
            throw new UnauthorizedAccessException("Only Staff can resolve guest feedback.");

        if (SelectedFeedback == null)
        {
            Notify("Please select a feedback first.", "warning");
            return;
        }

        // Keeps the selected ID and draft reply before database work.
        int id = SelectedFeedback.ComplaintId;
        string reply = ReplyText;

        // Resolving requires a non-empty reply.
        if (resolve && string.IsNullOrWhiteSpace(reply))
        {
            Notify("Please enter a reply before resolving.", "warning");
            return;
        }

        // Records the employee responsible for resolving the feedback.
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
            // Saves the trimmed reply and the employee who resolved it.
            feedback.Status = "Resolved";
            feedback.ReplyMessage = reply.Trim();
            feedback.ResolvedByEmployee = employeeId;
        }
        else
        {
            // Reopens the feedback while keeping its previously saved reply.
            feedback.Status = "Pending";
            feedback.ResolvedByEmployee = null;
        }

        await db.SaveChangesAsync();

        // Reloads saved data and restores the selected row.
        await LoadDataAsync();
        SelectedFeedback = Feedback.FirstOrDefault(f => f.ComplaintId == id);

        // Keeps the unsaved reply draft in the editor after marking pending.
        if (!resolve) ReplyText = reply;

        Notify(resolve ? "Feedback resolved successfully!" : "Feedback marked as pending.", "success");
    }
}
