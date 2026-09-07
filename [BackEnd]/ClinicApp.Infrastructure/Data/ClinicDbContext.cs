using System.Data;
using System.Text.Json;
using ClinicApp.Application.Interfaces;
using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Data;

public class ClinicDbContext : DbContext, IClinicDbContext
{
    public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options)
    {
    }

    public IQueryable<Role> Roles => Set<Role>();
    public IQueryable<User> Users => Set<User>();
    public IQueryable<Patient> Patients => Set<Patient>();
    public IQueryable<Doctor> Doctors => Set<Doctor>();
    public IQueryable<Appointment> Appointments => Set<Appointment>();
    public IQueryable<Invoice> Invoices => Set<Invoice>();
    public IQueryable<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public IQueryable<Payment> Payments => Set<Payment>();
    public IQueryable<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public IQueryable<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public IQueryable<AuditLog> AuditLogs => Set<AuditLog>();

    public async Task AddAsync<T>(T entity) where T : class
    {
        await Set<T>().AddAsync(entity);
    }

    public async Task AddRangeAsync<T>(IEnumerable<T> entities) where T : class
    {
        await Set<T>().AddRangeAsync(entities);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            await operation(cancellationToken);
            return;
        }

        // Avoid explicit user-initiated transactions to remain compatible with
        // EF Core execution strategies (SqlServerRetryingExecutionStrategy).
        // Operations will rely on SaveChanges to provide the necessary
        // transactional behavior for single-unit operations.
        await operation(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditing();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).ValueGeneratedNever();
            entity.Property(role => role.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(role => role.Name).IsUnique();
            entity.HasQueryFilter(role => !role.IsDeleted);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Username).IsRequired().HasMaxLength(100);
            entity.Property(user => user.PasswordHash).IsRequired().HasMaxLength(500);
            entity.HasIndex(user => user.Username).IsUnique();
            entity.HasOne(user => user.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(user => !user.IsDeleted);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(patient => patient.Id);
            entity.Property(patient => patient.FullName).IsRequired().HasMaxLength(200);
            entity.Property(patient => patient.PhoneNumber).IsRequired().HasMaxLength(50);
            entity.Property(patient => patient.Email).IsRequired().HasMaxLength(200);
            entity.Property(patient => patient.InsuranceInfo).HasMaxLength(500);
            entity.HasIndex(patient => patient.FullName);
            entity.HasIndex(patient => patient.Email).IsUnique();
            entity.HasQueryFilter(patient => !patient.IsDeleted);
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.ToTable("Doctors");
            entity.HasKey(doctor => doctor.Id);
            entity.Property(doctor => doctor.FullName).IsRequired().HasMaxLength(200);
            entity.Property(doctor => doctor.Specialty).IsRequired().HasMaxLength(200);
            entity.Property(doctor => doctor.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(doctor => doctor.Email).IsUnique();
            entity.HasQueryFilter(doctor => !doctor.IsDeleted);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(appointment => appointment.Id);
            entity.Property(appointment => appointment.Notes).HasMaxLength(2000);
            entity.HasOne(appointment => appointment.Patient)
                .WithMany()
                .HasForeignKey(appointment => appointment.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(appointment => appointment.Doctor)
                .WithMany()
                .HasForeignKey(appointment => appointment.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(appointment => new
            {
                appointment.DoctorId,
                appointment.StartTime,
                appointment.EndTime
            });
            entity.ToTable("Appointments", table => table.HasCheckConstraint(
                "CK_Appointments_TimeRange",
                "[EndTime] > [StartTime]"));
            entity.HasQueryFilter(appointment => !appointment.IsDeleted);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(invoice => invoice.Id);
            ConfigureMoney(entity.Property(invoice => invoice.TotalAmount));
            ConfigureMoney(entity.Property(invoice => invoice.VatRate));
            ConfigureMoney(entity.Property(invoice => invoice.VatAmount));
            ConfigureMoney(entity.Property(invoice => invoice.DiscountRate));
            ConfigureMoney(entity.Property(invoice => invoice.DiscountAmount));
            ConfigureMoney(entity.Property(invoice => invoice.NetAmount));
            entity.HasOne(invoice => invoice.Patient)
                .WithMany()
                .HasForeignKey(invoice => invoice.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(invoice => invoice.PatientId);
            entity.HasIndex(invoice => new { invoice.Status, invoice.InvoiceDate });
            entity.ToTable("Invoices", table => table.HasCheckConstraint(
                "CK_Invoices_Amounts",
                "[TotalAmount] >= 0 AND [VatAmount] >= 0 AND [DiscountAmount] >= 0 AND [NetAmount] >= 0"));
            entity.HasQueryFilter(invoice => !invoice.IsDeleted);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Description).IsRequired().HasMaxLength(300);
            ConfigureMoney(entity.Property(item => item.UnitPrice));
            ConfigureMoney(entity.Property(item => item.TotalPrice));
            entity.HasOne(item => item.Invoice)
                .WithMany(invoice => invoice.Items)
                .HasForeignKey(item => item.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable("InvoiceItems", table => table.HasCheckConstraint(
                "CK_InvoiceItems_Values",
                "[Quantity] > 0 AND [UnitPrice] > 0 AND [TotalPrice] > 0"));
            entity.HasQueryFilter(item => !item.IsDeleted);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            ConfigureMoney(entity.Property(payment => payment.Amount));
            entity.HasOne(payment => payment.Invoice)
                .WithMany(invoice => invoice.Payments)
                .HasForeignKey(payment => payment.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(payment => new { payment.InvoiceId, payment.PaidAt });
            entity.ToTable("Payments", table => table.HasCheckConstraint(
                "CK_Payments_Amount",
                "[Amount] > 0"));
            entity.HasQueryFilter(payment => !payment.IsDeleted);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.Token).IsRequired().HasMaxLength(500);
            entity.HasIndex(token => token.Token).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.ExpiresAt });
            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(token => !token.IsDeleted);
        });

        modelBuilder.Entity<PatientDocument>(entity =>
        {
            entity.ToTable("PatientDocuments");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.FileName).IsRequired().HasMaxLength(255);
            entity.Property(document => document.StoredFileName).IsRequired().HasMaxLength(255);
            entity.Property(document => document.ContentType).IsRequired().HasMaxLength(100);
            entity.HasOne(document => document.Patient)
                .WithMany(patient => patient.Documents)
                .HasForeignKey(document => document.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(document => document.PatientId);
            entity.HasQueryFilter(document => !document.IsDeleted);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(log => log.Id);
            entity.Property(log => log.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(log => log.EntityId).IsRequired().HasMaxLength(100);
            entity.Property(log => log.Action).IsRequired().HasMaxLength(50);
            entity.HasIndex(log => new { log.EntityName, log.EntityId });
            entity.HasIndex(log => log.CreatedAt);
        });
    }

    private void ApplyAuditing()
    {
        var now = DateTime.UtcNow;
        var logs = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries().Where(entry =>
                     entry.Entity is IAuditableEntity &&
                     entry.Entity is not AuditLog &&
                     entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var action = entry.State.ToString();
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                softDeletable.IsDeleted = true;
                entry.State = EntityState.Modified;
                action = "Deleted";
            }

            var auditable = (IAuditableEntity)entry.Entity;
            if (action == nameof(EntityState.Added))
            {
                auditable.CreatedAt = now;
            }
            auditable.UpdatedAt = now;

            var idProperty = entry.Properties.FirstOrDefault(property =>
                property.Metadata.Name == "Id");
            var changedProperties = entry.Properties
                .Where(property =>
                    action == nameof(EntityState.Added) ||
                    property.IsModified)
                .Select(property => property.Metadata.Name)
                .Where(name => name is not "PasswordHash" and not "Token")
                .ToArray();

            logs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = idProperty?.CurrentValue?.ToString() ?? string.Empty,
                Action = action,
                Changes = JsonSerializer.Serialize(changedProperties),
                CreatedAt = now
            });
        }

        if (logs.Count > 0)
        {
            Set<AuditLog>().AddRange(logs);
        }
    }

    private static void ConfigureMoney(
        Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<decimal> property)
    {
        property.HasPrecision(18, 2);
    }
}
