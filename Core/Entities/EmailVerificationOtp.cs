namespace Core.Entities;

public class EmailVerificationOtp : BaseEntity
{
    public int UserId { get; set; }
    public required string OtpHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime LastSentAt { get; set; }
    public int FailedAttempts { get; set; }
}
