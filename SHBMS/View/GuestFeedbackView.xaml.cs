using SHBMS.Helper;
using SHBMS.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SHBMS.View
{
    public partial class GuestFeedbackView : UserControl
    {
        private readonly ObservableCollection<GuestFeedback>
            _feedback = new();

        public GuestFeedbackView()
        {
            InitializeComponent();

            _feedback.Add(new GuestFeedback
            {
                ComplaintID = 1,
                CustomerID = "CUS001",
                GuestName = "Nguyen Minh Anh",
                Content = "Air conditioner was noisy.",
                Status = "Chưa xử lý"
            });

            _feedback.Add(new GuestFeedback
            {
                ComplaintID = 2,
                CustomerID = "CUS002",
                GuestName = "Tran Gia Bao",
                Content = "The room was very clean.",
                Status = "Đã xử lý",
                ReplyMessage = "Thank you for your feedback."
            });

            _feedback.Add(new GuestFeedback
            {
                ComplaintID = 3,
                CustomerID = "CUS003",
                GuestName = "Emily Johnson",
                Content = "Extra towels arrived late.",
                Status = "Đang xử lý"
            });

            FeedbackGrid.ItemsSource = _feedback;

            UpdateCount();
        }

        // Mark feedback as resolved
        private void Resolve_Click(
            object sender, RoutedEventArgs e)
        {

            if (FeedbackGrid.SelectedItem is not GuestFeedback f)
            {
                ToastService.Show(
                    "Please select a feedback first.",
                    "warning");
                return;
            }



            if (string.IsNullOrWhiteSpace(ReplyBox.Text))
            {
                ToastService.Show(
                    "Please enter a reply before resolving.",
                    "warning");
                return;
            }



            f.ReplyMessage = ReplyBox.Text.Trim();
            f.Status = "Đã xử lý";

            // Refresh DataGrid
            FeedbackGrid.Items.Refresh();
            // Update details
            DetailStatus.Text = f.Status;
            ReplyBox.Text = f.ReplyMessage;
            // Update statistics
            UpdateCount();
            ToastService.Show(
                "Feedback resolved successfully!",
                "success");
        }

        // Mark feedback as pending
        private void Pending_Click(
            object sender, RoutedEventArgs e)
        {
            if (FeedbackGrid.SelectedItem is not GuestFeedback f)
            {
                MessageBox.Show("Please select feedback.");
                return;
            }


            f.Status = "Chưa xử lý";

            // Refresh DataGrid
            FeedbackGrid.Items.Refresh();

            // Update status in details
            DetailStatus.Text = f.Status;

            // Update statistics
            UpdateCount();

        }

        // Search feedback
        private void Search_Click(
            object sender, RoutedEventArgs e)
        {
            string keyword = SearchBox.Text.Trim();

            FeedbackGrid.ItemsSource = _feedback.Where(f =>
                f.CustomerID.Contains(
                    keyword, StringComparison.OrdinalIgnoreCase) ||
                f.GuestName.Contains(
                    keyword, StringComparison.OrdinalIgnoreCase) ||
                f.Content.Contains(
                    keyword, StringComparison.OrdinalIgnoreCase) ||
                f.Status.Contains(
                    keyword, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            UpdateCount();
        }

        // Show all feedback
        private void ShowAll_Click(
            object sender, RoutedEventArgs e)
        {
            FeedbackGrid.ItemsSource = _feedback;
            SearchBox.Clear();

            UpdateCount();
        }

        // Display selected feedback

        private void FeedbackGrid_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (FeedbackGrid.SelectedItem is GuestFeedback feedback)
            {
                // Customer information
                DetailCustomerId.Text = feedback.CustomerID;
                DetailGuestName.Text = feedback.GuestName;
                DetailStatus.Text = feedback.Status;

                // Display full feedback content
                DetailFeedbackContent.Text = feedback.Content;

                // Existing reply
                ReplyBox.Text = feedback.ReplyMessage;
            }
            else
            {
                // Clear details when no feedback is selected
                DetailCustomerId.Text = "--";
                DetailGuestName.Text = "--";
                DetailStatus.Text = "--";

                DetailFeedbackContent.Text =
                    "Select a feedback to view its content.";

                ReplyBox.Clear();
            }
        }


        // Update feedback statistics
        private void UpdateCount()
        {
            int total = _feedback.Count;

            int resolved = _feedback.Count(f =>
                f.Status == "Đã xử lý");

            CountText.Text =
                $"Total: {total} | Resolved: {resolved}";
        }
    }
}
