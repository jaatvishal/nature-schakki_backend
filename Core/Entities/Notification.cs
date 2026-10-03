namespace Core.Entities;

public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; }
    public string? Link { get; set; }
}
