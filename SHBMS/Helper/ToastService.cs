using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SHBMS.Helper
{
    public static class ToastService
    {
        private static Border? _border;
        private static TextBlock? _title;
        private static TextBlock? _message;
        private static DispatcherTimer? _timer;

        public static void Initialize(
            Border border,
            TextBlock title,
            TextBlock message)
        {
            _timer?.Stop();

            if (_border != null)
            {
                _border.Visibility = Visibility.Collapsed;
            }

            _border = border;
            _title = title;
            _message = message;

            Panel.SetZIndex(border, 1000);
            border.IsHitTestVisible = false;
            border.Visibility = Visibility.Collapsed;
        }

        public static void Show(
            string message,
            string type = "success")
        {
            var border = _border;
            var title = _title;
            var text = _message;

            if (border == null || title == null || text == null ||
                string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            border.Dispatcher.Invoke(() =>
            {
                _timer?.Stop();

                switch ((type ?? "success").Trim().ToLowerInvariant())
                {
                    case "error":
                        title.Text = "Error";
                        border.Background = Brush("#FEEBEE");
                        border.BorderBrush = Brush("#F5C6CE");
                        title.Foreground = Brush("#B91C1C");
                        break;

                    case "warning":
                        title.Text = "Warning";
                        border.Background = Brush("#FFF8E1");
                        border.BorderBrush = Brush("#F3D99B");
                        title.Foreground = Brush("#946200");
                        break;

                    case "info":
                        title.Text = "Information";
                        border.Background = Brush("#EFF5FF");
                        border.BorderBrush = Brush("#D3E1FA");
                        title.Foreground = Brush("#4169E1");
                        break;

                    default:
                        title.Text = "Success";
                        border.Background = Brush("#E8F7EF");
                        border.BorderBrush = Brush("#D0ECDD");
                        title.Foreground = Brush("#15875A");
                        break;
                }

                text.Text = message;
                text.Foreground = Brush("#344766");

                border.Opacity = 1;
                border.Visibility = Visibility.Visible;

                if (_timer == null)
                {
                    _timer = new DispatcherTimer(
                        DispatcherPriority.Normal,
                        border.Dispatcher)
                    {
                        Interval = TimeSpan.FromSeconds(3)
                    };

                    _timer.Tick += (_, _) =>
                    {
                        _timer.Stop();

                        if (_border != null)
                        {
                            _border.Visibility = Visibility.Collapsed;
                        }
                    };
                }

                _timer.Start();
            });
        }

        private static SolidColorBrush Brush(string color)
        {
            return new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(color));
        }
    }
}