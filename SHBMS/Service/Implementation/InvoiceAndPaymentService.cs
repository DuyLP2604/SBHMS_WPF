using SHBMS.Model;
using SHBMS.DTO.Invoice;
using SHBMS.Service.Interface;

namespace SHBMS.Service.Implementation
{
    public class InvoiceAndPaymentService : IInvoiceAndPaymentService
    {
        private readonly Prn212SbhmsContext _context;
        public InvoiceAndPaymentService(Prn212SbhmsContext context) { _context = context; }

        public Task<InvoiceDto> CreateInvoiceAsync(string stayId) => throw new NotImplementedException();
        public Task<InvoiceDto> GetInvoiceByIdAsync(string invoiceId) => throw new NotImplementedException();
        public Task<bool> RecordPaymentAsync(PaymentCreateDto dto) => throw new NotImplementedException();
        public Task<IEnumerable<InvoiceDto>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task<IEnumerable<PaymentDto>> GetPaymentAuditLogsAsync() => throw new NotImplementedException();
    }
}
