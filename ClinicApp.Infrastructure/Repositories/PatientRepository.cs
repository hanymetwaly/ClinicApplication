using System.Linq.Expressions;
using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;
using ClinicApp.Domain.Interfaces;
using ClinicApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Repositories;

public class PatientRepository : Repository<Patient>, IPatientRepository
{
    public PatientRepository(ClinicDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Patient>> GetPagedAsync(string? search, int page, int pageSize, string? sortBy, bool descending)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(patient => patient.Documents)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.FullName.Contains(search) ||
                p.PhoneNumber.Contains(search) ||
                p.Email.Contains(search));
        }

        query = ApplySorting(query, sortBy, descending);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Patient>(items, totalCount, page, pageSize);
    }

    private IQueryable<Patient> ApplySorting(IQueryable<Patient> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLower() switch
        {
            "fullname" => descending ? query.OrderByDescending(p => p.FullName) : query.OrderBy(p => p.FullName),
            "phonenumber" => descending ? query.OrderByDescending(p => p.PhoneNumber) : query.OrderBy(p => p.PhoneNumber),
            "email" => descending ? query.OrderByDescending(p => p.Email) : query.OrderBy(p => p.Email),
            _ => descending ? query.OrderByDescending(p => p.FullName) : query.OrderBy(p => p.FullName)
        };
    }
}
