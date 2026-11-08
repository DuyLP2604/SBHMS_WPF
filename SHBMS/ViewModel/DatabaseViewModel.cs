using SHBMS.Helper;
using System.Windows.Input;
namespace SHBMS.ViewModel;
// Common loading/error handling. Each operation creates its own DbContext.
public abstract class DatabaseViewModel : BaseViewModel
{
    private bool _isBusy;
    private string _statusMessage = "";
    public bool IsBusy
    {
        get => _isBusy;
        private set { SetProperty(ref _isBusy, value); CommandManager.InvalidateRequerySuggested(); }
    }
    public string StatusMessage
    { get => _statusMessage; protected set => SetProperty(ref _statusMessage, value); }
    public RelayCommand RefreshCommand { get; }
    protected DatabaseViewModel()
        => RefreshCommand = new RelayCommand(async _ => await ReloadAsync(), _ => !IsBusy);
    public Task ReloadAsync() => RunAsync(LoadDataAsync);
    protected abstract Task LoadDataAsync();
    protected RelayCommand DatabaseCommand(Func<Task> action, Func<bool>? allowed = null)
        => new(async _ => await RunAsync(action), _ => !IsBusy && (allowed?.Invoke() ?? true));
    protected async Task RunAsync(Func<Task> action)
    {
        if (IsBusy) return;
        try { IsBusy = true; StatusMessage = ""; await action(); }
        catch (Exception ex) { StatusMessage = DatabaseErrorMessages.Describe(ex); }
        finally { IsBusy = false; }
    }
    protected static bool Contains(string? value, string keyword)
        => value?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;
}
