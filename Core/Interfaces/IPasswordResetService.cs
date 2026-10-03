namespace Core.Interfaces;

public interface IPasswordResetService
{
    Task RequestAsync(string email, CancellationToken cancellationToken = default);
    Task ResetAsync(string rawToken, string newPassword, CancellationToken cancellationToken = default);
}
