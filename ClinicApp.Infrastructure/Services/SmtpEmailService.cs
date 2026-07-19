using System.Net;
using System.Net.Mail;
using ClinicApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClinicApp.Infrastructure.Services;

#pragma warning disable SYSLIB0014

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_configuration["Smtp:Host"]);

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"];
        var port = _configuration.GetValue<int>("Smtp:Port", 587);
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var from = _configuration["Smtp:From"] ?? username;
        var enableSsl = _configuration.GetValue<bool>("Smtp:EnableSsl", true);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning("SMTP is not configured; email to {Recipient} was not sent.", to);
            return;
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password)
                ? new NetworkCredential(username, password)
                : null
        };

        var message = new MailMessage(from, to, subject, body) { IsBodyHtml = false };
        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Email sent to {Recipient} with subject {Subject}", to, subject);
    }
}
