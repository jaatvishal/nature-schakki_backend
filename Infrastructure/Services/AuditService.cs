using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class AuditService(StoreContext context) : IAuditService
{
    public async Task LogAsync(int? userId, string action, string entityType, int? entityId, string? details = null, string? ipAddress = null)
    {
        context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress
        });
        await context.SaveChangesAsync();
    }
}
