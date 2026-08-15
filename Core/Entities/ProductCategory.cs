namespace Core.Entities;

public class ProductCategory : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = [];
}
