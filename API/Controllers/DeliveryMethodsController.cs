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
        Ok(await context.DeliveryMethods.AsNoTracking().ToListAsync());
}
