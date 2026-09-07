using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;

namespace ClinicApp.Domain.Interfaces;

public interface IAppointmentRepository : IRepository<Appointment>
{
    Task<PagedResult<Appointment>> GetPagedAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? doctorId,
        AppointmentStatus? status,
        int page,
        int pageSize,
        string? sortBy,
        bool descending);
    Task<bool> IsSlotAvailableAsync(Guid doctorId, DateTime startTime, DateTime endTime, Guid? excludeAppointmentId = null);
}
