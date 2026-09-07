using ClinicApp.Domain.Entities;

namespace ClinicApp.Application.Interfaces;

public interface IClinicDbContext : IAsyncDisposable
{
    IQueryable<Role> Roles { get; }
    IQueryable<User> Users { get; }
    IQueryable<Patient> Patients { get; }
    IQueryable<Doctor> Doctors { get; }
    IQueryable<Appointment> Appointments { get; }
    IQueryable<Invoice> Invoices { get; }
    IQueryable<InvoiceItem> InvoiceItems { get; }
    IQueryable<Payment> Payments { get; }
    IQueryable<RefreshToken> RefreshTokens { get; }
    IQueryable<PatientDocument> PatientDocuments { get; }
    IQueryable<AuditLog> AuditLogs { get; }

    Task AddAsync<T>(T entity) where T : class;
    Task AddRangeAsync<T>(IEnumerable<T> entities) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
