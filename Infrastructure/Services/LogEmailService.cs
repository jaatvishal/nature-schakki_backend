using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class LogEmailService(ILogger<LogEmailService> logger) : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body)
    {
        logger.LogInformation("Email to {To}: {Subject} - {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
