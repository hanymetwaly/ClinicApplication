using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;

namespace ClinicApp.Domain.Interfaces;

public interface IPatientRepository : IRepository<Patient>
{
    Task<PagedResult<Patient>> GetPagedAsync(string? search, int page, int pageSize, string? sortBy, bool descending);
}
