using ClinicApp.Application.DTOs;
using ClinicApp.Application.Exceptions;
using ClinicApp.Application.Interfaces;
using ClinicApp.Application.Services;
using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;
using ClinicApp.Domain.Interfaces;
using ClinicApp.Infrastructure.Data;
using ClinicApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicApp.Api.Tests;

public class PatientServiceTests
{
    [Fact]
    public async Task CreatePatientAsync_ValidData_CreatesPatient()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            var request = new CreatePatientRequest
            {
                FullName = "John Doe",
                PhoneNumber = "0501234567",
                Email = "john@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1),
                MedicalHistory = "None",
                InsuranceInfo = "Basic"
            };

            var patient = await service.CreatePatientAsync(request);

            Assert.NotNull(patient);
            Assert.Equal("John Doe", patient.FullName);
            Assert.Equal("0501234567", patient.PhoneNumber);
            Assert.Equal("john@example.com", patient.Email);
        }
    }

    [Fact]
    public async Task CreatePatientAsync_DuplicateEmail_ThrowsException()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            var request = new CreatePatientRequest
            {
                FullName = "John Doe",
                PhoneNumber = "0501234567",
                Email = "john@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };

            await service.CreatePatientAsync(request);

            var request2 = new CreatePatientRequest
            {
                FullName = "Jane Doe",
                PhoneNumber = "0507654321",
                Email = "john@example.com",
                DateOfBirth = new DateOnly(1992, 5, 15)
            };

            // This should throw an exception due to unique constraint
            await Assert.ThrowsAsync<ConflictException>(() => service.CreatePatientAsync(request2));
        }
    }

    [Fact]
    public async Task UpdatePatientAsync_ValidData_UpdatesPatient()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            var created = await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "John Doe",
                PhoneNumber = "0501234567",
                Email = "john@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            });

            var updated = await service.UpdatePatientAsync(created.Id, new UpdatePatientRequest
            {
                FullName = "John Smith",
                PhoneNumber = "0507654321",
                Email = "john.smith@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1),
                MedicalHistory = "Diabetes",
                InsuranceInfo = "Premium"
            });

            Assert.NotNull(updated);
            Assert.Equal("John Smith", updated.FullName);
            Assert.Equal("0507654321", updated.PhoneNumber);
            Assert.Equal("john.smith@example.com", updated.Email);
            Assert.Equal("Diabetes", updated.MedicalHistory);
        }
    }

    [Fact]
    public async Task UpdatePatientAsync_DeletedPatient_ThrowsNotFound()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            var created = await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "John Doe",
                PhoneNumber = "0501234567",
                Email = "john@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            });

            await service.DeletePatientAsync(created.Id);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UpdatePatientAsync(created.Id, new UpdatePatientRequest
                {
                    FullName = "John Smith",
                    PhoneNumber = "0507654321",
                    Email = "john.smith@example.com",
                    DateOfBirth = new DateOnly(1990, 1, 1)
                }));
        }
    }

    [Fact]
    public async Task DeletePatientAsync_ExistingPatient_SoftDeletes()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            var created = await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "John Doe",
                PhoneNumber = "0501234567",
                Email = "john@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            });

            await service.DeletePatientAsync(created.Id);

            var deleted = await context.Patients.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == created.Id);
            Assert.NotNull(deleted);
            Assert.True(deleted.IsDeleted);
        }
    }

    [Fact]
    public async Task DeletePatientAsync_NonExistentPatient_ThrowsNotFound()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            await Assert.ThrowsAsync<NotFoundException>(() => service.DeletePatientAsync(Guid.NewGuid()));
        }
    }

    [Fact]
    public async Task DeletePatientAsync_AlreadyDeleted_ThrowsNotFound()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            var created = await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "John Doe",
                PhoneNumber = "0501234567",
                Email = "john@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            });

            await service.DeletePatientAsync(created.Id);
            await Assert.ThrowsAsync<NotFoundException>(() => service.DeletePatientAsync(created.Id));
        }
    }

    [Fact]
    public async Task GetPatientsAsync_WithSearch_ReturnsMatchingPatients()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "Alice Johnson",
                PhoneNumber = "0501111111",
                Email = "alice@example.com",
                DateOfBirth = new DateOnly(1985, 5, 20)
            });

            await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "Bob Smith",
                PhoneNumber = "0502222222",
                Email = "bob@example.com",
                DateOfBirth = new DateOnly(1990, 3, 15)
            });

            await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "Alice Williams",
                PhoneNumber = "0503333333",
                Email = "alice.w@example.com",
                DateOfBirth = new DateOnly(1988, 8, 10)
            });

            var result = await service.GetPatientsAsync(search: "Alice");
            Assert.Equal(2, result.Items.Count());
        }
    }

    [Fact]
    public async Task GetPatientsAsync_WithPagination_ReturnsCorrectPage()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            for (int i = 1; i <= 14; i++)
            {
                await service.CreatePatientAsync(new CreatePatientRequest
                {
                    FullName = $"Patient {i}",
                    PhoneNumber = $"050{i:D8}",
                    Email = $"patient{i}@example.com",
                    DateOfBirth = new DateOnly(1990, 1, 1)
                });
            }

            var page1 = await service.GetPatientsAsync(page: 1, pageSize: 10);
            var page2 = await service.GetPatientsAsync(page: 2, pageSize: 10);

            Assert.Equal(10, page1.Items.Count());
            Assert.Equal(5, page2.Items.Count());
            Assert.Equal(15, page1.TotalCount);
        }
    }

    [Fact]
    public async Task GetPatientsAsync_SortedByFullName_ReturnsSortedResults()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "Charlie Brown",
                PhoneNumber = "0501111111",
                Email = "charlie@example.com",
                DateOfBirth = new DateOnly(1985, 5, 20)
            });

            await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "Alice Johnson",
                PhoneNumber = "0502222222",
                Email = "alice@example.com",
                DateOfBirth = new DateOnly(1990, 3, 15)
            });

            await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "Bob Smith",
                PhoneNumber = "0503333333",
                Email = "bob@example.com",
                DateOfBirth = new DateOnly(1988, 8, 10)
            });

            var result = await service.GetPatientsAsync(sortBy: "fullName", descending: false);
            var names = result.Items.Select(p => p.FullName).ToList();

            Assert.Equal("Alice Johnson", names[0]);
            Assert.Equal("Bob Smith", names[1]);
            Assert.Equal("Charlie Brown", names[2]);
        }
    }

    [Fact]
    public async Task UploadPatientDocumentAsync_ValidFile_CreatesDocument()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("report data"));
            var document = await service.UploadPatientDocumentAsync(patient.Id, "report.txt", "text/plain", content.Length, content);

            Assert.NotNull(document);
            Assert.Equal("report.txt", document.FileName);
            Assert.Equal("text/plain", document.ContentType);

            var patientDto = await service.GetPatientAsync(patient.Id);
            Assert.Equal(1, patientDto.DocumentCount);
        }
    }

    [Fact]
    public async Task GetPatientsAsync_SoftDeletedExcluded_DoesNotReturnDeleted()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            var patient1 = await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "Active Patient",
                PhoneNumber = "0501111111",
                Email = "active@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            });

            var patient2 = await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "Deleted Patient",
                PhoneNumber = "0502222222",
                Email = "deleted@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            });

            await service.DeletePatientAsync(patient2.Id);

            var result = await service.GetPatientsAsync();
            Assert.Equal(2, result.Items.Count);
            Assert.DoesNotContain(result.Items, p => p.FullName == "Deleted Patient");
        }
    }
}
