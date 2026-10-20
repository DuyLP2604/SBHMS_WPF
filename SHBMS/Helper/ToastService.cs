
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SHBMS.Helper
{
    public static class ToastService
    {
        private static DispatcherTimer? _timer;

        public static void Show(
            string message,
            string type = "success")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow
                    is not FrameworkElement window)
                    return;

                // Find toast elements in MainWindow
                var toastBorder =
                    window.FindName("ToastBorder") as Border;

                var toastText =
                    window.FindName("ToastMessage") as TextBlock;

                var toastTitle =
                    window.FindName("ToastTitle") as TextBlock;

                if (toastBorder == null ||
                    toastText == null ||
                    toastTitle == null)
                    return;

                _timer?.Stop();

                toastText.Text = message;

                switch (type.ToLower())
                {
                    case "error":
                        toastTitle.Text = "Error";
                        toastBorder.Background =
                            new SolidColorBrush(
                                Color.FromRgb(254, 235, 238));
                        toastTitle.Foreground =
                            Brushes.Firebrick;
                        break;

                    case "warning":
                        toastTitle.Text = "Warning";
                        toastBorder.Background =
                            new SolidColorBrush(
                                Color.FromRgb(255, 248, 225));
                        toastTitle.Foreground =
                            Brushes.DarkGoldenrod;
                        break;

                    default:
                        toastTitle.Text = "Success";
                        toastBorder.Background =
                            new SolidColorBrush(
                                Color.FromRgb(232, 247, 239));
                        toastTitle.Foreground =
                            new SolidColorBrush(
                                Color.FromRgb(21, 135, 90));
                        break;
                }

                toastBorder.Visibility = Visibility.Visible;

                // Auto hide after 3 seconds
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };

                _timer.Tick += (s, e) =>
                {
                    _timer.Stop();
                    toastBorder.Visibility =
                        Visibility.Collapsed;
                };

                _timer.Start();
            });
        }
    }
}
