using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class RegisterResultDto
{
    public string Message { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class VerifyEmailOtpDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required, RegularExpression(@"^\d{6}$")]
    public string Otp { get; set; } = string.Empty;
}

public class ResendEmailOtpDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class AddressDto
{
    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    [Required, StringLength(200)]
    public string Street { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string State { get; set; } = string.Empty;
    [Required, StringLength(20)]
    public string ZipCode { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string Country { get; set; } = string.Empty;
}

public class CreateOrderDto
{
    [Required]
    public AddressDto ShipToAddress { get; set; } = new();
    [Range(1, int.MaxValue)]
    public int DeliveryMethodId { get; set; }
    public string? CouponCode { get; set; }
    [Required]
    public string PaymentMethod { get; set; } = "COD";
}

public class OrderDto
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public AddressDto ShippingAddress { get; set; } = new();
    public string DeliveryMethod { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DeliveryCost { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<OrderItemDto> OrderItems { get; set; } = [];
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string PictureUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class CreateReviewDto
{
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class ReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateCouponDto
{
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public int? MaxUsageCount { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class CouponDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public bool IsActive { get; set; }
}

public class PaymentIntentDto
{
    public PaymentIntentDto(string clientSecret, string paymentIntentId)
    {
        ClientSecret = clientSecret;
        PaymentIntentId = paymentIntentId;
    }

    public string ClientSecret { get; set; }
    public string PaymentIntentId { get; set; }
}

public class RefreshTokenDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string PictureUrl { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
}
