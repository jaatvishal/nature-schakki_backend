using System.Net;
using System.Net.Mail;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class SmtpEmailService(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.FromAddress) ||
            string.IsNullOrWhiteSpace(settings.Smtp.Username) ||
            string.IsNullOrWhiteSpace(settings.Smtp.Password))
        {
            logger.LogError("SMTP email provider is not configured.");
            throw new ServiceUnavailableException("Verification email is temporarily unavailable. Please try again.");
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(settings.FromAddress, settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(to);

            using var client = new SmtpClient(settings.Smtp.Host, settings.Smtp.Port)
            {
                EnableSsl = settings.Smtp.EnableSsl,
                Credentials = new NetworkCredential(settings.Smtp.Username, settings.Smtp.Password)
            };

            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("Verification email sent to user.");
        }
        catch (ServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email provider failed while sending a verification email.");
            throw new ServiceUnavailableException("Verification email is temporarily unavailable. Please try again.");
        }
    }
}
