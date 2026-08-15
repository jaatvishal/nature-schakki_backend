using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class NotificationService(StoreContext context) : INotificationService
{
    public async Task<Notification> CreateAsync(int userId, string title, string message, string? link = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Link = link,
            IsRead = false
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        return notification;
    }

    public async Task<IReadOnlyList<Notification>> GetForUserAsync(int userId) =>
        await context.Notifications.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync();

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await context.Notifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId)
            ?? throw new Core.Exceptions.NotFoundException("Notification not found.");

        notification.IsRead = true;
        await context.SaveChangesAsync();
    }
}
