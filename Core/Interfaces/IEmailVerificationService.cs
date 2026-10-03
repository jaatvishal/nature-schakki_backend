namespace Core.Interfaces;

public interface IEmailVerificationService
{
    Task SendOtpAsync(int userId, string email, bool enforceCooldown, CancellationToken cancellationToken = default);
    Task VerifyOtpAsync(int userId, string otp, CancellationToken cancellationToken = default);
}
