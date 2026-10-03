namespace Core.Entities;

public class Wishlist : BaseEntity
{
    public int UserId { get; set; }
    public ICollection<WishlistItem> Items { get; set; } = [];
}
