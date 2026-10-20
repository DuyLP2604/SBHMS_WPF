namespace SHBMS.ViewModel;

public abstract class OperationViewModel : BaseViewModel
{
    private string _message = "";
    public bool IsBusy => false;
    public bool IsError => false;
    public string Message { get => _message; protected set => SetProperty(ref _message, value); }
}
