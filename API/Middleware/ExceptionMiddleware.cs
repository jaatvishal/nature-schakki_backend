using Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware;

public class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var statusCode = ex switch
        {
            ApiException apiEx => apiEx.StatusCode,
            Microsoft.EntityFrameworkCore.DbUpdateException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode >= 500)
            logger.LogError(ex, "Unhandled request failure for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        else
            logger.LogWarning("Request failed with status {StatusCode} for {Method} {Path}",
                statusCode, context.Request.Method, context.Request.Path);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = ex is ApiException
                ? ex.Message
                : statusCode == StatusCodes.Status409Conflict
                    ? "The request conflicts with the current data state."
                    : "An unexpected error occurred.",
            Instance = context.Request.Path
        };

        if (ex is ValidationException validationEx)
            problem.Extensions["errors"] = validationEx.Errors;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(problem);
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        409 => "Conflict",
        404 => "Not Found",
        429 => "Too Many Requests",
        503 => "Service Unavailable",
        _ => "Internal Server Error"
    };
}
