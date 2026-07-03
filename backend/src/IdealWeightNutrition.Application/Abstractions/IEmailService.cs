namespace IdealWeightNutrition.Application.Abstractions;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);

    Task SendWithAttachmentAsync(
        string to,
        string subject,
        string htmlBody,
        byte[] attachment,
        string attachmentFileName,
        CancellationToken cancellationToken = default);
}
