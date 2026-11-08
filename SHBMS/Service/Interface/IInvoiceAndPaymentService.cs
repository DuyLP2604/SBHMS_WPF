using SHBMS.DTO.Invoice;
namespace SHBMS.Service.Interface
{
    public interface IInvoiceAndPaymentService
    {
        // [UC-09] Manage Invoices
        Task<InvoiceDto> CreateInvoiceAsync(string stayId);
        Task<InvoiceDto> GetInvoiceByIdAsync(string invoiceId);

        // [UC-10] Record Payment
        Task<bool> RecordPaymentAsync(PaymentCreateDto dto);

        // [UC-11] Review Invoices & Payments (Manager)
        Task<IEnumerable<InvoiceDto>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<PaymentDto>> GetPaymentAuditLogsAsync();
    }
}
