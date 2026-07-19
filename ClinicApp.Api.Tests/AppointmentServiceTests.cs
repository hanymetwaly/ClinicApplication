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

public class AppointmentServiceTests
{
    [Fact]
    public async Task BookAppointmentAsync_ValidSlot_CreatesAppointment()
    {
        var (context, service, patient, doctor) = TestClinicFactory.Create();
        await using (context)
        {
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
            var appointment = await service.BookAppointmentAsync(new CreateAppointmentRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = start,
                EndTime = start.AddHours(1),
                Notes = "Regular checkup"
            });

            Assert.NotNull(appointment);
            Assert.Equal(patient.Id, appointment.PatientId);
            Assert.Equal(doctor.Id, appointment.DoctorId);
            Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
            Assert.Equal("Regular checkup", appointment.Notes);
        }
    }

    [Fact]
    public async Task BookAppointmentAsync_OverlappingSlot_ThrowsConflict()
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

            await Assert.ThrowsAsync<ConflictException>(() =>
                service.BookAppointmentAsync(new CreateAppointmentRequest
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    StartTime = start.AddMinutes(30),
                    EndTime = start.AddHours(2)
                }));
        }
    }

    [Fact]
    public async Task BookAppointmentAsync_DifferentDoctor_SameSlot_Allowed()
    {
        var (context, service, patient, doctor) = TestClinicFactory.Create();
        await using (context)
        {
            var doctor2 = new Doctor
            {
                Id = Guid.NewGuid(),
                FullName = "Dr. Test 2",
                Specialty = "General",
                Email = "doctor2@example.com"
            };
            await context.AddAsync(doctor2);
            await context.SaveChangesAsync();

            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
            var appointment1 = await service.BookAppointmentAsync(new CreateAppointmentRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = start,
                EndTime = start.AddHours(1)
            });

            var appointment2 = await service.BookAppointmentAsync(new CreateAppointmentRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor2.Id,
                StartTime = start,
                EndTime = start.AddHours(1)
            });

            Assert.NotNull(appointment1);
            Assert.NotNull(appointment2);
        }
    }

    [Fact]
    public async Task CancelAppointmentAsync_ExistingAppointment_CancelsSuccessfully()
    {
        var (context, service, patient, doctor) = TestClinicFactory.Create();
        await using (context)
        {
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
            var appointment = await service.BookAppointmentAsync(new CreateAppointmentRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = start,
                EndTime = start.AddHours(1)
            });

            var cancelled = await service.CancelAppointmentAsync(appointment.Id);

            Assert.NotNull(cancelled);
            Assert.Equal(AppointmentStatus.Cancelled, cancelled.Status);
        }
    }

    [Fact]
    public async Task CancelAppointmentAsync_NonExistentAppointment_ThrowsNotFound()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            await Assert.ThrowsAsync<NotFoundException>(() => service.CancelAppointmentAsync(Guid.NewGuid()));
        }
    }

    [Fact]
    public async Task CancelAppointmentAsync_AlreadyCancelled_ReturnsCancelled()
    {
        var (context, service, patient, doctor) = TestClinicFactory.Create();
        await using (context)
        {
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
            var appointment = await service.BookAppointmentAsync(new CreateAppointmentRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = start,
                EndTime = start.AddHours(1)
            });

            await service.CancelAppointmentAsync(appointment.Id);
            var cancelled = await service.CancelAppointmentAsync(appointment.Id);

            Assert.NotNull(cancelled);
            Assert.Equal(AppointmentStatus.Cancelled, cancelled.Status);
        }
    }

    [Fact]
    public async Task RescheduleAppointmentAsync_ValidSlot_ReschedulesSuccessfully()
    {
        var (context, service, patient, doctor) = TestClinicFactory.Create();
        await using (context)
        {
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
            var appointment = await service.BookAppointmentAsync(new CreateAppointmentRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = start,
                EndTime = start.AddHours(1)
            });

            var newStart = start.AddDays(1).AddHours(14);
            var rescheduled = await service.RescheduleAppointmentAsync(appointment.Id, new RescheduleAppointmentRequest
            {
                StartTime = newStart,
                EndTime = newStart.AddHours(1)
            });

            Assert.NotNull(rescheduled);
            Assert.Equal(newStart, rescheduled.StartTime);
        }
    }

    [Fact]
    public async Task RescheduleAppointmentAsync_OverlappingSlot_ThrowsConflict()
    {
        var (context, service, patient, doctor) = TestClinicFactory.Create();
        await using (context)
        {
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
            var appointment1 = await service.BookAppointmentAsync(new CreateAppointmentRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = start,
                EndTime = start.AddHours(1)
            });

            var appointment2 = await service.BookAppointmentAsync(new CreateAppointmentRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = start.AddHours(2),
                EndTime = start.AddHours(3)
            });

            await Assert.ThrowsAsync<ConflictException>(() =>
                service.RescheduleAppointmentAsync(appointment2.Id, new RescheduleAppointmentRequest
                {
                    StartTime = start.AddMinutes(30),
                    EndTime = start.AddHours(1)
                }));
        }
    }

    [Fact]
    public async Task GetAppointmentsAsync_WithDateFilter_ReturnsFilteredResults()
    {
        var (context, service, patient, doctor) = TestClinicFactory.Create();
        await using (context)
        {
            var date1 = DateTime.UtcNow.Date.AddDays(1);
            var date2 = DateTime.UtcNow.Date.AddDays(2);

            await service.BookAppointmentAsync(new CreateAppointmentRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = date1.AddHours(9),
                EndTime = date1.AddHours(10)
            });

            await service.BookAppointmentAsync(new CreateAppointmentRequest
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = date2.AddHours(14),
                EndTime = date2.AddHours(15)
            });

            var result = await service.GetAppointmentsAsync(date1, date1.AddDays(1));
            Assert.Single(result.Items);
        }
    }

    [Fact]
    public async Task GetAppointmentsAsync_Pagination_ReturnsCorrectPage()
    {
        var (context, service, patient, doctor) = TestClinicFactory.Create();
        await using (context)
        {
            var baseDate = DateTime.UtcNow.Date.AddDays(1);
            for (int i = 0; i < 15; i++)
            {
                await service.BookAppointmentAsync(new CreateAppointmentRequest
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    StartTime = baseDate.AddHours(i),
                    EndTime = baseDate.AddHours(i + 1)
                });
            }

            var page1 = await service.GetAppointmentsAsync(page: 1, pageSize: 10);
            var page2 = await service.GetAppointmentsAsync(page: 2, pageSize: 10);

            Assert.Equal(10, page1.Items.Count());
            Assert.Equal(5, page2.Items.Count());
        }
    }

    [Fact]
    public async Task GetDoctorsAsync_ReturnsAllDoctors()
    {
        var (context, service, _, _) = TestClinicFactory.Create();
        await using (context)
        {
            var doctors = await service.GetDoctorsAsync();
            Assert.Single(doctors);
            Assert.Equal("Dr. Test", doctors.First().FullName);
        }
    }
}
