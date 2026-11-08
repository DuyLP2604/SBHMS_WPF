using System.Windows.Controls;
using SHBMS.ViewModel;

namespace SHBMS.View
{
    public partial class RevenueManagementPage : UserControl
    {
        public RevenueManagementPage()
        {
            InitializeComponent();
            DataContext = new RevenueManagementViewModel();
        }
    }
}
