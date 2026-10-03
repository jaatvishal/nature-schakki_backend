using Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class IndiaTimeZoneService : IApplicationTimeZone
{
    private readonly TimeZoneInfo _timeZone;

    public IndiaTimeZoneService(IConfiguration configuration)
    {
        var configuredId = configuration["ApplicationTimeZone:Id"] ?? "Asia/Kolkata";
        _timeZone = Resolve(configuredId);
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime ToDisplayTime(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind switch
        {
            DateTimeKind.Utc => utcDateTime,
            DateTimeKind.Local => utcDateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc)
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utc, _timeZone);
    }

    public DateTime ToUtc(DateTime displayDateTime)
    {
        var unspecified = DateTime.SpecifyKind(displayDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, _timeZone);
    }

    private static TimeZoneInfo Resolve(string configuredId)
    {
        foreach (var id in new[] { configuredId, "Asia/Kolkata", "India Standard Time" }.Distinct())
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        throw new InvalidOperationException(
            $"Unable to resolve the configured application time zone '{configuredId}'.");
    }
}
