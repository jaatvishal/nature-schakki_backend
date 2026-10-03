using Core.Entities;
using Core.Enums;

namespace Core.Interfaces;

public interface IReviewService
{
    Task<ProductReview> CreateReviewAsync(int userId, int productId, int rating, string? comment);
    Task<IReadOnlyList<ProductReview>> GetProductReviewsAsync(int productId);
    Task<ProductReview> ModerateReviewAsync(int reviewId, ReviewStatus status);
}
