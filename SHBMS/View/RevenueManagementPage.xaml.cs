using System.Windows.Controls;
using SHBMS.ViewModel;

namespace SHBMS.View
{
    /// <summary>
    /// Interaction logic for RevenueManagementPage.xaml
    /// </summary>
    public partial class RevenueManagementPage : UserControl
    {
        public RevenueManagementPage()
        {
            InitializeComponent();
            DataContext = new RevenueManagementViewModel();
        }
    }
}
