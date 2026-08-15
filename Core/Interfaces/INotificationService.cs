using Core.Entities;

namespace Core.Interfaces;

public interface INotificationService
{
    Task<Notification> CreateAsync(int userId, string title, string message, string? link = null);
    Task<IReadOnlyList<Notification>> GetForUserAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
}
