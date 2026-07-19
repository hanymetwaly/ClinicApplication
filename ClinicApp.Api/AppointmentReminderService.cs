using ClinicApp.Application.Interfaces;
using ClinicApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClinicApp.Api;

public class AppointmentReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentReminderService> _logger;

    public AppointmentReminderService(IServiceProvider serviceProvider, ILogger<AppointmentReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendRemindersAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send appointment reminders.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task SendRemindersAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IClinicDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        if (!emailService.IsConfigured)
        {
            _logger.LogInformation("Email service is not configured; skipping appointment reminders.");
            return;
        }

        var windowStart = DateTime.UtcNow;
        var windowEnd = windowStart.AddHours(24);

        var appointments = await context.Appointments
            .Include(appointment => appointment.Patient)
            .Include(appointment => appointment.Doctor)
            .Where(appointment =>
                appointment.Status == AppointmentStatus.Scheduled &&
                appointment.StartTime > windowStart &&
                appointment.StartTime <= windowEnd &&
                appointment.ReminderSentAt == null)
            .ToListAsync(cancellationToken);

        foreach (var appointment in appointments)
        {
            var patient = appointment.Patient;
            var doctor = appointment.Doctor;
            if (patient == null || string.IsNullOrWhiteSpace(patient.Email))
            {
                continue;
            }

            var subject = "Upcoming appointment reminder";
            var body = $"Dear {patient.FullName},\n\nThis is a reminder that you have an appointment with {doctor?.FullName ?? "your doctor"} on {appointment.StartTime:yyyy-MM-dd HH:mm} UTC.\n\nClinicApp";

            try
            {
                await emailService.SendEmailAsync(patient.Email, subject, body, cancellationToken);
                appointment.ReminderSentAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send reminder for appointment {AppointmentId}.", appointment.Id);
            }
        }
    }
}
