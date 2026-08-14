namespace Core.Entities;

public class Product : BaseEntity
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public required string PictureUrl { get; set; }
    public required string Type { get; set; }
    public required string Brand { get; set; }
    public int QuantityInStock { get; set; }
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public ProductCategory? Category { get; set; }
    public ProductBrand? ProductBrand { get; set; }
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductReview> Reviews { get; set; } = [];
    public Inventory? Inventory { get; set; }
}
