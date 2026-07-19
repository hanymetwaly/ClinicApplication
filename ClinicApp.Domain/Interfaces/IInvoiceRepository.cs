using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;

namespace ClinicApp.Domain.Interfaces;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<PagedResult<Invoice>> GetPagedAsync(
        Guid? patientId,
        InvoiceStatus? status,
        int page,
        int pageSize,
        string? sortBy,
        bool descending);
}
