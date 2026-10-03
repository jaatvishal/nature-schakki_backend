using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Middleware;

public class ActiveUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppIdentityDbContext identityContext)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            var isActive = await identityContext.Users.AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => x.IsActive)
                .FirstOrDefaultAsync();
            if (!isActive)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    status = StatusCodes.Status401Unauthorized,
                    title = "Unauthorized",
                    detail = "This account is inactive."
                });
                return;
            }
        }

        await next(context);
    }
}
