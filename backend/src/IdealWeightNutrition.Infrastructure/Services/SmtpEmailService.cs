using System.Net;
using System.Net.Mail;
using IdealWeightNutrition.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var smtpHost = _configuration["Smtp:Host"];
        var smtpUsername = _configuration["Smtp:Username"];

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername))
        {
            _logger.LogInformation(
                "SMTP not configured — email not sent to {To}. Subject: {Subject}. Body: {Body}",
                to,
                subject,
                htmlBody);
            return;
        }

        var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var smtpPassword = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:FromEmail"] ?? smtpUsername;
        var fromName = _configuration["Smtp:FromName"] ?? "Ideal Weight Nutrition";
        var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUsername, smtpPassword),
            EnableSsl = enableSsl,
            Timeout = 30_000
        };

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Email sent to {To}", to);
    }

    public async Task SendWithAttachmentAsync(
        string to,
        string subject,
        string htmlBody,
        byte[] attachment,
        string attachmentFileName,
        CancellationToken cancellationToken = default)
    {
        var smtpHost = _configuration["Smtp:Host"];
        var smtpUsername = _configuration["Smtp:Username"];

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername))
        {
            _logger.LogInformation(
                "SMTP not configured — email with attachment not sent to {To}. Subject: {Subject}",
                to,
                subject);
            return;
        }

        var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var smtpPassword = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:FromEmail"] ?? smtpUsername;
        var fromName = _configuration["Smtp:FromName"] ?? "Ideal Weight Nutrition";
        var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);
        message.Attachments.Add(new Attachment(new MemoryStream(attachment), attachmentFileName, "application/pdf"));

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUsername, smtpPassword),
            EnableSsl = enableSsl,
            Timeout = 30_000
        };

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Email with attachment sent to {To}", to);
    }
}
