using System;
using System.Threading.Tasks;
using System.Windows.Input;
using SHBMS.Helper;

namespace SHBMS.ViewModel
{
    public abstract class DatabaseViewModel : BaseViewModel
    {
        private bool _isBusy;
        private string _statusMessage = "";

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                SetProperty(ref _isBusy, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            protected set
            {
                SetProperty(ref _statusMessage, value);

                // Existing ViewModels can continue using StatusMessage.
                if (!string.IsNullOrWhiteSpace(value))
                {
                    ToastService.Show(value, "info");
                }
            }
        }

        public RelayCommand RefreshCommand { get; }

        protected DatabaseViewModel()
        {
            RefreshCommand = new RelayCommand(
                async _ => await ReloadAsync(),
                _ => !IsBusy);
        }

        public Task ReloadAsync()
        {
            return RunAsync(LoadDataAsync);
        }

        protected abstract Task LoadDataAsync();

        protected RelayCommand DatabaseCommand(
            Func<Task> action,
            Func<bool>? allowed = null)
        {
            return new RelayCommand(
                async _ => await RunAsync(action),
                _ => !IsBusy && (allowed?.Invoke() ?? true));
        }

        protected void Notify(
            string message,
            string type = "info")
        {
            // Update the backing field directly to avoid showing two toasts.
            SetProperty(
                ref _statusMessage,
                message,
                nameof(StatusMessage));

            ToastService.Show(message, type);
        }

        protected async Task RunAsync(Func<Task> action)
        {
            if (IsBusy)
            {
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "";

                await action();
            }
            catch (Exception ex)
            {
                Notify(
                    DatabaseErrorMessages.Describe(ex),
                    "error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected static bool Contains(
            string? value,
            string keyword)
        {
            return value?.Contains(
                keyword,
                StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}