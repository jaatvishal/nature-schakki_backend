using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ReviewService(StoreContext context) : IReviewService
{
    public async Task<ProductReview> CreateReviewAsync(int userId, int productId, int rating, string? comment)
    {
        if (rating is < 1 or > 5)
            throw new BadRequestException("Rating must be between 1 and 5.");

        var productExists = await context.Products.AnyAsync(x => x.Id == productId);
        if (!productExists)
            throw new NotFoundException($"Product {productId} not found.");

        var review = new ProductReview
        {
            UserId = userId,
            ProductId = productId,
            Rating = rating,
            Comment = comment,
            Status = ReviewStatus.Pending
        };

        context.ProductReviews.Add(review);
        await context.SaveChangesAsync();
        return review;
    }

    public async Task<IReadOnlyList<ProductReview>> GetProductReviewsAsync(int productId) =>
        await context.ProductReviews
            .AsNoTracking()
            .Where(x => x.ProductId == productId && x.Status == ReviewStatus.Approved)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

    public async Task<ProductReview> ModerateReviewAsync(int reviewId, ReviewStatus status)
    {
        var review = await context.ProductReviews.FindAsync(reviewId)
            ?? throw new NotFoundException($"Review {reviewId} not found.");

        review.Status = status;
        review.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return review;
    }
}
