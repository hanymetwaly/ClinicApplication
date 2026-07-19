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

public class InvoiceServiceTests
{
    [Fact]
    public async Task CreateInvoiceAsync_ValidItems_CalculatesCorrectTotals()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            var request = new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                VatRate = 15,
                DiscountRate = 5,
                Items =
                [
                    new InvoiceItemRequest { Description = "Consultation", Quantity = 1, UnitPrice = 100 },
                    new InvoiceItemRequest { Description = "Lab test", Quantity = 2, UnitPrice = 50 }
                ]
            };

            var invoice = await service.CreateInvoiceAsync(request);

            Assert.NotNull(invoice);
            Assert.Equal(200m, invoice.TotalAmount);
            Assert.Equal(30m, invoice.VatAmount);
            Assert.Equal(10m, invoice.DiscountAmount);
            Assert.Equal(220m, invoice.NetAmount);
            Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        }
    }

    [Fact]
    public async Task CreateInvoiceAsync_EmptyItems_ThrowsException()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            var request = new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                Items = []
            };

            await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateInvoiceAsync(request));
        }
    }

    [Fact]
    public async Task CreateInvoiceAsync_ZeroQuantity_ThrowsException()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            var request = new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                Items =
                [
                    new InvoiceItemRequest { Description = "Consultation", Quantity = 0, UnitPrice = 100 }
                ]
            };

            await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateInvoiceAsync(request));
        }
    }

    [Fact]
    public async Task CreateInvoiceAsync_NegativePrice_ThrowsException()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            var request = new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                Items =
                [
                    new InvoiceItemRequest { Description = "Consultation", Quantity = 1, UnitPrice = -100 }
                ]
            };

            await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateInvoiceAsync(request));
        }
    }

    [Fact]
    public async Task PayInvoiceAsync_PartialPayment_UpdatesStatusToPartial()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            var invoice = await service.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                Items =
                [
                    new InvoiceItemRequest { Description = "Consultation", Quantity = 1, UnitPrice = 100 }
                ]
            });

            var payment = await service.PayInvoiceAsync(invoice.Id, 50m);

            Assert.NotNull(payment);
            Assert.Equal(50m, payment.Amount);

            var updatedInvoice = await service.GetInvoiceAsync(invoice.Id);
            Assert.Equal(InvoiceStatus.Partial, updatedInvoice.Status);
        }
    }

    [Fact]
    public async Task PayInvoiceAsync_FullPayment_UpdatesStatusToPaid()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            var invoice = await service.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                VatRate = 0,
                DiscountRate = 0,
                Items =
                [
                    new InvoiceItemRequest { Description = "Consultation", Quantity = 1, UnitPrice = 100 }
                ]
            });

            var payment = await service.PayInvoiceAsync(invoice.Id, 100m);

            Assert.NotNull(payment);

            var updatedInvoice = await service.GetInvoiceAsync(invoice.Id);
            Assert.Equal(InvoiceStatus.Paid, updatedInvoice.Status);
        }
    }

    [Fact]
    public async Task PayInvoiceAsync_ExceedsBalance_ThrowsException()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            var invoice = await service.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                Items =
                [
                    new InvoiceItemRequest { Description = "Consultation", Quantity = 1, UnitPrice = 100 }
                ]
            });

            await Assert.ThrowsAsync<RequestValidationException>(() =>
                service.PayInvoiceAsync(invoice.Id, 200m));
        }
    }

    [Fact]
    public async Task PayInvoiceAsync_DeletedInvoice_ThrowsException()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            var invoice = await service.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                Items =
                [
                    new InvoiceItemRequest { Description = "Consultation", Quantity = 1, UnitPrice = 100 }
                ]
            });

            // Soft delete the invoice
            var invoiceEntity = await context.Invoices.IgnoreQueryFilters().FirstAsync(i => i.Id == invoice.Id);
            invoiceEntity.IsDeleted = true;
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.PayInvoiceAsync(invoice.Id, 50m));
        }
    }

    [Fact]
    public async Task GetInvoicesAsync_WithPagination_ReturnsCorrectPage()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            for (int i = 1; i <= 15; i++)
            {
                await service.CreateInvoiceAsync(new CreateInvoiceRequest
                {
                    PatientId = patient.Id,
                    Items =
                    [
                        new InvoiceItemRequest { Description = $"Service {i}", Quantity = 1, UnitPrice = 100 }
                    ]
                });
            }

            var page1 = await service.GetInvoicesAsync(page: 1, pageSize: 10);
            var page2 = await service.GetInvoicesAsync(page: 2, pageSize: 10);

            Assert.Equal(10, page1.Items.Count());
            Assert.Equal(5, page2.Items.Count());
            Assert.Equal(15, page1.TotalCount);
        }
    }

    [Fact]
    public async Task GetInvoicesAsync_SortedByTotalAmount_ReturnsSortedResults()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            await service.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                Items =
                [
                    new InvoiceItemRequest { Description = "Small", Quantity = 1, UnitPrice = 50 }
                ]
            });

            await service.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                Items =
                [
                    new InvoiceItemRequest { Description = "Large", Quantity = 1, UnitPrice = 200 }
                ]
            });

            await service.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                Items =
                [
                    new InvoiceItemRequest { Description = "Medium", Quantity = 1, UnitPrice = 100 }
                ]
            });

            var result = await service.GetInvoicesAsync(sortBy: "totalAmount", descending: true);
            var amounts = result.Items.Select(i => i.TotalAmount).ToList();

            Assert.Equal(200m, amounts[0]);
            Assert.Equal(100m, amounts[1]);
            Assert.Equal(50m, amounts[2]);
        }
    }

    [Fact]
    public async Task GetInvoiceAsync_WithItems_ReturnsInvoiceWithItems()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            var invoice = await service.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                Items =
                [
                    new InvoiceItemRequest { Description = "Consultation", Quantity = 1, UnitPrice = 100 },
                    new InvoiceItemRequest { Description = "Lab test", Quantity = 2, UnitPrice = 50 }
                ]
            });

            var retrieved = await service.GetInvoiceAsync(invoice.Id);

            Assert.NotNull(retrieved);
            Assert.Equal(2, retrieved.Items.Count());
        }
    }

    [Fact]
    public async Task GetInvoiceAsync_NonExistentInvoice_ThrowsNotFound()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetInvoiceAsync(Guid.NewGuid()));
        }
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsCorrectMetrics()
    {
        var (context, service, patient, doctor) = TestClinicFactory.Create();
        await using (context)
        {
            // Create some test data
            await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "Test Patient 2",
                PhoneNumber = "0509999999",
                Email = "test2@example.com",
                DateOfBirth = new DateOnly(1990, 1, 1)
            });

            var start = DateTime.UtcNow.Date.AddHours(9);
            await service.BookAppointmentAsync(new CreateAppointmentRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = start,
                EndTime = start.AddHours(1)
            });

            var invoice = await service.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                Items =
                [
                    new InvoiceItemRequest { Description = "Consultation", Quantity = 1, UnitPrice = 100 }
                ]
            });

            await service.PayInvoiceAsync(invoice.Id, 105m);

            var dashboard = await service.GetDashboardAsync();

            Assert.NotNull(dashboard);
            Assert.True(dashboard.TotalPatients >= 2);
            Assert.True(dashboard.TodayAppointments >= 1);
            Assert.True(dashboard.Revenue > 0);
        }
    }
}
