using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;
using ClinicApp.Domain.Interfaces;
using ClinicApp.Infrastructure.Data;
using ClinicApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Api.Tests;

public class RepositoryTests
{
    [Fact]
    public async Task PatientRepository_AddAsync_AddsPatient()
    {
        var context = CreateTestContext();
        await using (context)
        {
            var repository = new PatientRepository(context);
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FullName = "Test Patient",
                PhoneNumber = "0501234567",
                Email = "test@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };

            await repository.AddAsync(patient);
            await context.SaveChangesAsync();

            var retrieved = await context.Patients.FirstOrDefaultAsync(p => p.Id == patient.Id);
            Assert.NotNull(retrieved);
            Assert.Equal("Test Patient", retrieved.FullName);
        }
    }

    [Fact]
    public async Task PatientRepository_GetByIdAsync_ReturnsPatient()
    {
        var context = CreateTestContext();
        await using (context)
        {
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FullName = "Test Patient",
                PhoneNumber = "0501234567",
                Email = "test@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };
            await context.AddAsync(patient);
            await context.SaveChangesAsync();

            var repository = new PatientRepository(context);
            var retrieved = await repository.GetByIdAsync(patient.Id);

            Assert.NotNull(retrieved);
            Assert.Equal("Test Patient", retrieved.FullName);
        }
    }

    [Fact]
    public async Task PatientRepository_GetPagedAsync_WithSearch_ReturnsFilteredResults()
    {
        var context = CreateTestContext();
        await using (context)
        {
            await context.AddRangeAsync(
                new Patient
                {
                    Id = Guid.NewGuid(),
                    FullName = "Alice Johnson",
                    PhoneNumber = "0501111111",
                    Email = "alice@example.com",
                    DateOfBirth = new DateOnly(1985, 5, 20)
                },
                new Patient
                {
                    Id = Guid.NewGuid(),
                    FullName = "Bob Smith",
                    PhoneNumber = "0502222222",
                    Email = "bob@example.com",
                    DateOfBirth = new DateOnly(1990, 3, 15)
                },
                new Patient
                {
                    Id = Guid.NewGuid(),
                    FullName = "Alice Williams",
                    PhoneNumber = "0503333333",
                    Email = "alice.w@example.com",
                    DateOfBirth = new DateOnly(1988, 8, 10)
                });
            await context.SaveChangesAsync();

            var repository = new PatientRepository(context);
            var result = await repository.GetPagedAsync(search: "Alice", page: 1, pageSize: 10, sortBy: "fullName", descending: false);

            Assert.Equal(2, result.Items.Count());
        }
    }

    [Fact]
    public async Task PatientRepository_UpdateAsync_UpdatesPatient()
    {
        var context = CreateTestContext();
        await using (context)
        {
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FullName = "Test Patient",
                PhoneNumber = "0501234567",
                Email = "test@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };
            await context.AddAsync(patient);
            await context.SaveChangesAsync();

            var repository = new PatientRepository(context);
            patient.FullName = "Updated Patient";
            await repository.UpdateAsync(patient);
            await context.SaveChangesAsync();

            var retrieved = await context.Patients.FirstOrDefaultAsync(p => p.Id == patient.Id);
            Assert.NotNull(retrieved);
            Assert.Equal("Updated Patient", retrieved.FullName);
        }
    }

    [Fact]
    public async Task AppointmentRepository_IsSlotAvailableAsync_OverlappingSlot_ReturnsFalse()
    {
        var context = CreateTestContext();
        await using (context)
        {
            var doctor = new Doctor
            {
                Id = Guid.NewGuid(),
                FullName = "Dr. Test",
                Specialty = "General",
                Email = "doctor@example.com"
            };
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FullName = "Test Patient",
                PhoneNumber = "0501234567",
                Email = "test@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = start,
                EndTime = start.AddHours(1),
                Status = AppointmentStatus.Scheduled
            };
            await context.AddRangeAsync(doctor, patient, appointment);
            await context.SaveChangesAsync();

            var repository = new AppointmentRepository(context);
            var isAvailable = await repository.IsSlotAvailableAsync(
                doctor.Id,
                start.AddMinutes(30),
                start.AddHours(2));

            Assert.False(isAvailable);
        }
    }

    [Fact]
    public async Task AppointmentRepository_IsSlotAvailableAsync_DifferentDoctor_ReturnsTrue()
    {
        var context = CreateTestContext();
        await using (context)
        {
            var doctor1 = new Doctor
            {
                Id = Guid.NewGuid(),
                FullName = "Dr. Test 1",
                Specialty = "General",
                Email = "doctor1@example.com"
            };
            var doctor2 = new Doctor
            {
                Id = Guid.NewGuid(),
                FullName = "Dr. Test 2",
                Specialty = "General",
                Email = "doctor2@example.com"
            };
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FullName = "Test Patient",
                PhoneNumber = "0501234567",
                Email = "test@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = doctor1.Id,
                StartTime = start,
                EndTime = start.AddHours(1),
                Status = AppointmentStatus.Scheduled
            };
            await context.AddRangeAsync(doctor1, doctor2, patient, appointment);
            await context.SaveChangesAsync();

            var repository = new AppointmentRepository(context);
            var isAvailable = await repository.IsSlotAvailableAsync(
                doctor2.Id,
                start.AddMinutes(30),
                start.AddHours(2));

            Assert.True(isAvailable);
        }
    }

    [Fact]
    public async Task AppointmentRepository_IsSlotAvailableAsync_ExcludesSpecifiedAppointment()
    {
        var context = CreateTestContext();
        await using (context)
        {
            var doctor = new Doctor
            {
                Id = Guid.NewGuid(),
                FullName = "Dr. Test",
                Specialty = "General",
                Email = "doctor@example.com"
            };
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FullName = "Test Patient",
                PhoneNumber = "0501234567",
                Email = "test@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = start,
                EndTime = start.AddHours(1),
                Status = AppointmentStatus.Scheduled
            };
            await context.AddRangeAsync(doctor, patient, appointment);
            await context.SaveChangesAsync();

            var repository = new AppointmentRepository(context);
            var isAvailable = await repository.IsSlotAvailableAsync(
                doctor.Id,
                start.AddMinutes(30),
                start.AddHours(2),
                appointment.Id);

            Assert.True(isAvailable);
        }
    }

    [Fact]
    public async Task InvoiceRepository_GetPagedAsync_WithPagination_ReturnsCorrectPage()
    {
        var context = CreateTestContext();
        await using (context)
        {
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FullName = "Test Patient",
                PhoneNumber = "0501234567",
                Email = "test@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };
            await context.AddAsync(patient);

            for (int i = 1; i <= 15; i++)
            {
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient.Id,
                    Items =
                    [
                        new InvoiceItem
                        {
                            Id = Guid.NewGuid(),
                            Description = $"Service {i}",
                            Quantity = 1,
                            UnitPrice = 100,
                            TotalPrice = 100
                        }
                    ],
                    TotalAmount = 100,
                    VatAmount = 15,
                    DiscountAmount = 5,
                    NetAmount = 110,
                    Status = InvoiceStatus.Draft
                };
                await context.AddAsync(invoice);
            }
            await context.SaveChangesAsync();

            var repository = new InvoiceRepository(context);
            var page1 = await repository.GetPagedAsync(patientId: null, status: null, page: 1, pageSize: 10, sortBy: "invoiceDate", descending: true);
            var page2 = await repository.GetPagedAsync(patientId: null, status: null, page: 2, pageSize: 10, sortBy: "invoiceDate", descending: true);

            Assert.Equal(10, page1.Items.Count());
            Assert.Equal(5, page2.Items.Count());
        }
    }

    [Fact]
    public async Task Repository_GenericMethods_WorkCorrectly()
    {
        var context = CreateTestContext();
        await using (context)
        {
            var repository = new Repository<Patient>(context);
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FullName = "Test Patient",
                PhoneNumber = "0501234567",
                Email = "test@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };

            await repository.AddAsync(patient);
            await context.SaveChangesAsync();

            var retrieved = await repository.GetByIdAsync(patient.Id);
            Assert.NotNull(retrieved);

            var all = await repository.GetAllAsync();
            Assert.Single(all);

            patient.FullName = "Updated";
            await repository.UpdateAsync(patient);
            await context.SaveChangesAsync();

            var updated = await repository.GetByIdAsync(patient.Id);
            Assert.NotNull(updated);
            Assert.Equal("Updated", updated.FullName);

            await repository.DeleteAsync(patient);
            await context.SaveChangesAsync();

            var deleted = await repository.GetByIdAsync(patient.Id);
            Assert.Null(deleted);
        }
    }

    private static ClinicDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ClinicDbContext(options);
    }
}
