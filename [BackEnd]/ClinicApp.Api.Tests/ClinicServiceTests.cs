using ClinicApp.Application.DTOs;
using ClinicApp.Application.Exceptions;
using ClinicApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Api.Tests;

public class ClinicServiceTests
{
    [Fact]
    public async Task PatientCrudSupportsSearchAndSoftDelete()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            var created = await service.CreatePatientAsync(new CreatePatientRequest
            {
                FullName = "Alice Walker",
                PhoneNumber = "0501111111",
                Email = "alice@example.com",
                DateOfBirth = new DateOnly(1985, 5, 20),
                MedicalHistory = "Diabetes",
                InsuranceInfo = "Premium"
            });

            var search = await service.GetPatientsAsync("Alice");
            Assert.Single(search.Items);

            var updated = await service.UpdatePatientAsync(created.Id, new UpdatePatientRequest
            {
                FullName = "Alice Smith",
                PhoneNumber = "0502222222",
                Email = "alice.smith@example.com",
                DateOfBirth = new DateOnly(1985, 5, 20),
                MedicalHistory = "Diabetes, controlled",
                InsuranceInfo = "Premium"
            });
            Assert.Equal("Alice Smith", updated.FullName);

            await service.DeletePatientAsync(created.Id);
            Assert.Empty((await service.GetPatientsAsync("Alice")).Items);
            Assert.True(await context.Patients.IgnoreQueryFilters()
                .Where(patient => patient.Id == created.Id)
                .Select(patient => patient.IsDeleted)
                .SingleAsync());
        }
    }

    [Fact]
    public async Task BookAppointmentRejectsOverlappingDoctorSlot()
    {
        var (context, service, patient, doctor) = TestClinicFactory.Create();
        await using (context)
        {
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
            await service.BookAppointmentAsync(new CreateAppointmentRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = start,
                EndTime = start.AddHours(1)
            });

            var exception = await Assert.ThrowsAsync<ConflictException>(() =>
                service.BookAppointmentAsync(new CreateAppointmentRequest
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    StartTime = start.AddMinutes(30),
                    EndTime = start.AddHours(2)
                }));

            Assert.Contains("already has an appointment", exception.Message);
        }
    }

    [Fact]
    public async Task InvoiceCalculatesMultipleItemsAndTracksPartialThenPaidStatus()
    {
        var (context, service, patient, _) = TestClinicFactory.Create();
        await using (context)
        {
            var invoice = await service.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                PatientId = patient.Id,
                VatRate = 15,
                DiscountRate = 5,
                Items =
                [
                    new InvoiceItemRequest { Description = "Consultation", Quantity = 1, UnitPrice = 100 },
                    new InvoiceItemRequest { Description = "Lab test", Quantity = 2, UnitPrice = 50 }
                ]
            });

            Assert.Equal(200m, invoice.TotalAmount);
            Assert.Equal(30m, invoice.VatAmount);
            Assert.Equal(10m, invoice.DiscountAmount);
            Assert.Equal(220m, invoice.NetAmount);

            await service.PayInvoiceAsync(invoice.Id, 100m);
            Assert.Equal(InvoiceStatus.Partial, (await service.GetInvoiceAsync(invoice.Id)).Status);

            await service.PayInvoiceAsync(invoice.Id, 120m);
            var paidInvoice = await service.GetInvoiceAsync(invoice.Id);
            Assert.Equal(InvoiceStatus.Paid, paidInvoice.Status);
            Assert.Equal(0m, paidInvoice.OutstandingAmount);
        }
    }

    [Fact]
    public async Task PaymentCannotExceedOutstandingBalance()
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
                service.PayInvoiceAsync(invoice.Id, invoice.NetAmount + 1));
        }
    }
}
