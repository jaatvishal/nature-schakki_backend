using API;
using API.Middleware;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, config) =>
        config.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

    ValidateProductionConfiguration(builder.Configuration, builder.Environment);
    builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

    var jwtKey = builder.Configuration["JwtSettings:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey))
    {
        if (!builder.Environment.IsDevelopment())
            throw new InvalidOperationException("JwtSettings:Key must be configured.");

        jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        builder.Configuration["JwtSettings:Key"] = jwtKey;
        Log.Warning("JwtSettings:Key is missing; using an ephemeral development key.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                ValidAudience = builder.Configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddVersionedApiExplorer();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(SwaggerConfiguration.Configure);

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (allowedOrigins.Length > 0)
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        });
    });

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddRateLimiter(options =>
    {
        var testing = builder.Environment.IsEnvironment("Testing");
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = testing ? 10_000 : 100,
                    Window = TimeSpan.FromMinutes(1)
                }));
        options.AddPolicy("password-reset", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = testing ? 10_000 : 5,
                    Window = TimeSpan.FromMinutes(15)
                }));
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<StoreContext>()
        .AddDbContextCheck<AppIdentityDbContext>();

    var app = builder.Build();

    app.UseForwardedHeaders();
    if (!app.Environment.IsDevelopment())
        app.UseHsts();

    app.Use(async (context, next) =>
    {
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        await next();
    });
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionMiddleware>();

    if (app.Environment.IsDevelopment() ||
        builder.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseMiddleware<ActiveUserMiddleware>();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var storeContext = scope.ServiceProvider.GetRequiredService<StoreContext>();
        var identityContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

        if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            await storeContext.Database.MigrateAsync();
            await identityContext.Database.MigrateAsync();
        }

        if (builder.Configuration.GetValue<bool>("Database:SeedStoreDataOnStartup"))
            await StoreContextSeed.SeedAsync(storeContext);

        if (builder.Configuration.GetValue("Database:SeedIdentityRolesOnStartup", true))
            await IdentitySeed.SeedUsersAsync(scope.ServiceProvider, app.Environment);
    }

    app.MapFallback(async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var webRoot = app.Environment.WebRootPath
            ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        var indexPath = Path.Combine(webRoot, "index.html");
        if (!File.Exists(indexPath))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(indexPath);
    });

    app.Run();
}
catch (HostAbortedException)
{
    // Expected when EF Core tooling stops the host after creating services.
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static void ValidateProductionConfiguration(IConfiguration configuration, IHostEnvironment environment)
{
    if (!environment.IsProduction()) return;

    var errors = new List<string>();
    RequireConnectionString("DefaultConnection");

    Require("JwtSettings:Key", value => Encoding.UTF8.GetByteCount(value) >= 32);
    RequireHttpsUrl("JwtSettings:Issuer");
    RequireHttpsUrl("JwtSettings:Audience");
    RequireHttpsUrl("ClientUrl");
    Require("Email:FromAddress");
    Require("Email:Smtp:Username");
    Require("Email:Smtp:Password");

    if (configuration["CacheProvider"]?.Equals("Redis", StringComparison.OrdinalIgnoreCase) == true)
        RequireConnectionString("Redis");
    if (configuration["FileStorage:Provider"]?.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase) == true)
        RequireConnectionString("BlobStorage");
    if (configuration.GetValue<bool>("SeedUsers:Enabled"))
    {
        Require("SeedUsers:AdminEmail");
        Require("SeedUsers:AdminPassword", value => value.Length >= 12);
    }

    if (errors.Count > 0)
        throw new InvalidOperationException(
            "Production configuration is incomplete: " + string.Join(", ", errors));

    void Require(string key, Func<string, bool>? validator = null)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value) || (validator != null && !validator(value)))
            errors.Add(key);
    }

    void RequireConnectionString(string name)
    {
        var value = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(value) ||
            value.Contains("YOUR_PASSWORD", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            errors.Add($"ConnectionStrings:{name}");
    }

    void RequireHttpsUrl(string key)
    {
        var value = configuration[key];
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            errors.Add(key);
    }
}

public partial class Program;
