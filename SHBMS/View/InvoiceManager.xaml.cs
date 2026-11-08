using SHBMS.ViewModel;
using System.Windows.Controls;
namespace SHBMS.View;

public partial class InvoiceManager : UserControl
{
    public InvoiceManager()
    {
        InitializeComponent();

        var viewModel = new ViewModel.InvoiceViewModel();
        DataContext = viewModel;

        // Gọi load dữ liệu sau khi View đã vẽ xong
        this.Loaded += (s, e) =>
        {
            viewModel.ReloadAsync();
        };
    }
}