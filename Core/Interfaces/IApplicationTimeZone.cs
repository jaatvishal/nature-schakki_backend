namespace Core.Interfaces;

public interface IApplicationTimeZone
{
    DateTime UtcNow { get; }
    DateTime ToDisplayTime(DateTime utcDateTime);
    DateTime ToUtc(DateTime displayDateTime);
}
