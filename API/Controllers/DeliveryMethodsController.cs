using Asp.Versioning;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class DeliveryMethodsController(StoreContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDeliveryMethods() =>
        Ok(await context.DeliveryMethods.AsNoTracking()
            .OrderBy(x => x.Price)
            .Take(1)
            .Select(x => new
            {
                x.Id,
                ShortName = "Standard Delivery",
                Description = "Delivery within 7 days. Available in Delhi NCR and Ghaziabad areas only.",
                DeliveryTimeDays = 7,
                x.Price
            })
            .ToListAsync());
}
