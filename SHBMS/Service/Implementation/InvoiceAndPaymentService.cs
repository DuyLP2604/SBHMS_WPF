using SBHMS.DTO.Invoice;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation
{
    public class InvoiceAndPaymentService : IInvoiceAndPaymentService
    {
        private readonly PRN212_SBHMSContext _context;
        public InvoiceAndPaymentService(PRN212_SBHMSContext context) { _context = context; }

        public Task<InvoiceDto> CreateInvoiceAsync(int stayId) => throw new NotImplementedException();
        public Task<InvoiceDto> GetInvoiceByIdAsync(int invoiceId) => throw new NotImplementedException();
        public Task<bool> RecordPaymentAsync(PaymentCreateDto dto) => throw new NotImplementedException();
        public Task<IEnumerable<InvoiceDto>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task<IEnumerable<PaymentDto>> GetPaymentAuditLogsAsync() => throw new NotImplementedException();
    }
}