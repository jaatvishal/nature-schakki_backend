using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CartItemChangeDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int QuantityKg { get; set; }

    [Required]
    [RegularExpression("^(add|set)$")]
    public string Mode { get; set; } = "set";
}
