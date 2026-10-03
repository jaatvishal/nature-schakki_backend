using Asp.Versioning;
using Core.DTOs;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpGet("product/{productId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductReviews(int productId) =>
        Ok(await reviewService.GetProductReviewsAsync(productId));

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateReview(CreateReviewDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var review = await reviewService.CreateReviewAsync(userId, dto.ProductId, dto.Rating, dto.Comment);
        return Ok(review);
    }
}
