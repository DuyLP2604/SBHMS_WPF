using System;
using System.Threading.Tasks;
using System.Windows.Input;
using SHBMS.Helper;

namespace SHBMS.ViewModel
{
    public abstract class DatabaseViewModel : BaseViewModel
    {
        // Prevent overlapping commands and repeated clicks while an operation is running.
        private bool _isBusy;
        // Keep compatibility with older pages that still bind to StatusMessage.
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

        /// <summary>
        /// Create a refresh command that waits for the current operation to finish.
        /// </summary>
        protected DatabaseViewModel()
        {
            RefreshCommand = new RelayCommand(
                async _ => await ReloadAsync(),
                _ => !IsBusy);
        }

        /// <summary>
        /// Reload page data through the shared busy and error handling flow.
        /// </summary>
        public Task ReloadAsync()
        {
            return RunAsync(LoadDataAsync);
        }

        /// <summary>
        /// Let each page define which data it loads from the database.
        /// </summary>
        protected abstract Task LoadDataAsync();

        /// <summary>
        /// Wrap an async action with busy checks and an optional permission rule.
        /// </summary>
        protected RelayCommand DatabaseCommand(
            Func<Task> action,
            Func<bool>? allowed = null)
        {
            return new RelayCommand(
                async _ => await RunAsync(action),
                _ => !IsBusy && (allowed?.Invoke() ?? true));
        }

        /// <summary>
        /// Show one toast and keep the same message available for data binding.
        /// </summary>
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

        /// <summary>
        /// Run one operation at a time and convert failures into readable toasts.
        /// </summary>
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

        /// <summary>
        /// Compare search text without depending on upper or lower case.
        /// </summary>
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
