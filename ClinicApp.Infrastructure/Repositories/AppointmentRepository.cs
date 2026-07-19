using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;
using ClinicApp.Domain.Interfaces;
using ClinicApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Repositories;

public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(ClinicDbContext context) : base(context)
    {
    }

    public override async Task<Appointment?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(appointment => appointment.Patient)
            .Include(appointment => appointment.Doctor)
            .FirstOrDefaultAsync(appointment => appointment.Id == id);
    }

    public async Task<PagedResult<Appointment>> GetPagedAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? doctorId,
        AppointmentStatus? status,
        int page,
        int pageSize,
        string? sortBy,
        bool descending)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(appointment => appointment.Patient)
            .Include(appointment => appointment.Doctor)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(appointment => appointment.StartTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(appointment => appointment.StartTime < endDate.Value);
        }

        if (doctorId.HasValue)
        {
            query = query.Where(appointment => appointment.DoctorId == doctorId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(appointment => appointment.Status == status.Value);
        }

        query = ApplySorting(query, sortBy, descending);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Appointment>(items, totalCount, page, pageSize);
    }

    public async Task<bool> IsSlotAvailableAsync(Guid doctorId, DateTime startTime, DateTime endTime, Guid? excludeAppointmentId = null)
    {
        var query = _dbSet.Where(a =>
            a.DoctorId == doctorId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.StartTime < endTime &&
            a.EndTime > startTime);

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAppointmentId.Value);
        }

        return !await query.AnyAsync();
    }

    private IQueryable<Appointment> ApplySorting(IQueryable<Appointment> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLower() switch
        {
            "starttime" => descending ? query.OrderByDescending(a => a.StartTime) : query.OrderBy(a => a.StartTime),
            "status" => descending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            _ => descending ? query.OrderByDescending(a => a.StartTime) : query.OrderBy(a => a.StartTime)
        };
    }
}
