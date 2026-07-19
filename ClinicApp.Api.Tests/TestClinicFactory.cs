using ClinicApp.Application.Interfaces;
using ClinicApp.Application.Services;
using ClinicApp.Domain.Entities;
using ClinicApp.Domain.Interfaces;
using ClinicApp.Infrastructure.Data;
using ClinicApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicApp.Api.Tests;

internal static class TestClinicFactory
{
    public static (
        ClinicDbContext Context,
        ClinicService Service,
        Patient Patient,
        Doctor Doctor) Create()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ClinicDbContext(options);
        IPatientRepository patientRepository = new PatientRepository(context);
        IAppointmentRepository appointmentRepository = new AppointmentRepository(context);
        IInvoiceRepository invoiceRepository = new InvoiceRepository(context);
        IPasswordHasher passwordHasher = new PasswordHasher();
        IFileStorageService fileStorage = new TestFileStorageService();
        var service = new ClinicService(
            context,
            NullLogger<ClinicService>.Instance,
            patientRepository,
            appointmentRepository,
            invoiceRepository,
            passwordHasher,
            fileStorage);

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FullName = "Test Patient",
            PhoneNumber = "0500000000",
            Email = "patient@example.com",
            DateOfBirth = new DateOnly(1990, 1, 1),
            MedicalHistory = "None",
            InsuranceInfo = "Standard"
        };
        var doctor = new Doctor
        {
            Id = Guid.NewGuid(),
            FullName = "Dr. Test",
            Specialty = "General",
            Email = "doctor@example.com"
        };
        context.AddRange(patient, doctor);
        context.SaveChanges();

        return (context, service, patient, doctor);
    }
}
