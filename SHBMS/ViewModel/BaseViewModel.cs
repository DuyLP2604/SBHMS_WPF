using System.ComponentModel;
using System.Runtime.CompilerServices;
using SHBMS.Helper;

namespace SHBMS.ViewModel;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    private readonly List<IRefreshCommand> _commands = new();
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        RefreshCommands();
    }

    protected RelayCommand Command(Action execute, Func<bool>? canExecute = null)
    {
        var command = new RelayCommand(execute, canExecute);
        _commands.Add(command);
        return command;
    }

    public void RefreshCommands()
    {
        foreach (var command in _commands) command.Refresh();
    }
}
