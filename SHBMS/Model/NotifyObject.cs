using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SHBMS.Model
{
    /// <summary>
    /// Base class so the UI updates automatically when a property changes
    /// (room tiles change colour, grid rows refresh, ...).
    /// </summary>
    public abstract class NotifyObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void Raise([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            Raise(name);
            return true;
        }
    }
}
