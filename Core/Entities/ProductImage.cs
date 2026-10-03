namespace Core.Entities;

public class ProductImage : BaseEntity
{
    public int ProductId { get; set; }
    public required string Url { get; set; }
    public bool IsMain { get; set; }
    public Product? Product { get; set; }
}
