namespace Infrastructure.Options;

public class EmailOptions
{
    public const string SectionName = "Email";
    public string Provider { get; set; } = "Smtp";
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Nature's Chakki";
    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class EmailVerificationOptions
{
    public const string SectionName = "EmailVerification";
    public int OtpLifetimeMinutes { get; set; } = 10;
    public int MaxAttempts { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
}

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string Provider { get; set; } = "Local";
}
