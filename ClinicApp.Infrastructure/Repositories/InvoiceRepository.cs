using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;
using ClinicApp.Domain.Interfaces;
using ClinicApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Repositories;

public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(ClinicDbContext context) : base(context)
    {
    }

    public override async Task<Invoice?> GetByIdAsync(Guid id)
    {
        var invoice = await _dbSet
            .AsNoTracking()
            .Include(invoice => invoice.Items)
            .Include(invoice => invoice.Payments)
            .FirstOrDefaultAsync(invoice => invoice.Id == id);

        if (invoice is not null)
        {
            invoice.Patient = await GetPatientNameAsync(invoice.PatientId);
        }

        return invoice;
    }

    public async Task<PagedResult<Invoice>> GetPagedAsync(
        Guid? patientId,
        InvoiceStatus? status,
        int page,
        int pageSize,
        string? sortBy,
        bool descending)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(invoice => invoice.PatientId == patientId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(invoice => invoice.Status == status.Value);
        }

        query = ApplySorting(query, sortBy, descending);

        var totalCount = await query.CountAsync();
        var ids = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(invoice => invoice.Id)
            .ToListAsync();

        var items = await _dbSet
            .AsNoTracking()
            .Where(invoice => ids.Contains(invoice.Id))
            .Include(invoice => invoice.Items)
            .Include(invoice => invoice.Payments)
            .ToListAsync();

        var patientIds = items.Select(invoice => invoice.PatientId).Distinct().ToList();
        var patients = await _context.Patients
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(patient => patientIds.Contains(patient.Id))
            .Select(patient => new { patient.Id, patient.FullName })
            .ToDictionaryAsync(patient => patient.Id, patient => patient.FullName);

        foreach (var invoice in items)
        {
            invoice.Patient = patients.TryGetValue(invoice.PatientId, out var fullName)
                ? new Patient { FullName = fullName }
                : null;
        }

        items = sortBy?.ToLower() switch
        {
            "totalamount" => descending ? items.OrderByDescending(i => i.TotalAmount).ToList() : items.OrderBy(i => i.TotalAmount).ToList(),
            "status" => descending ? items.OrderByDescending(i => i.Status).ToList() : items.OrderBy(i => i.Status).ToList(),
            _ => descending ? items.OrderByDescending(i => i.InvoiceDate).ToList() : items.OrderBy(i => i.InvoiceDate).ToList()
        };

        return new PagedResult<Invoice>(items, totalCount, page, pageSize);
    }

    private IQueryable<Invoice> ApplySorting(IQueryable<Invoice> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLower() switch
        {
            "invoicedate" => descending ? query.OrderByDescending(i => i.InvoiceDate) : query.OrderBy(i => i.InvoiceDate),
            "totalamount" => descending ? query.OrderByDescending(i => i.TotalAmount) : query.OrderBy(i => i.TotalAmount),
            "status" => descending ? query.OrderByDescending(i => i.Status) : query.OrderBy(i => i.Status),
            _ => descending ? query.OrderByDescending(i => i.InvoiceDate) : query.OrderBy(i => i.InvoiceDate)
        };
    }

    private async Task<Patient?> GetPatientNameAsync(Guid patientId)
    {
        var fullName = await _context.Patients
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(patient => patient.Id == patientId)
            .Select(patient => patient.FullName)
            .FirstOrDefaultAsync();

        return fullName is null ? null : new Patient { FullName = fullName };
    }
}
