using API.Middleware;
using Asp.Versioning;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "NaturesChakki API", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithOrigins(
                    "http://localhost:5001",
                    "https://localhost:5001",
                    "http://localhost:4200",
                    "https://localhost:4200"));
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                }));
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<StoreContext>()
        .AddDbContextCheck<AppIdentityDbContext>();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
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

    using (var scope = app.Services.CreateScope())
    {
        if (!app.Environment.IsEnvironment("Testing"))
        {
            var storeContext = scope.ServiceProvider.GetRequiredService<StoreContext>();
            var identityContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
            await storeContext.Database.MigrateAsync();
            await identityContext.Database.MigrateAsync();
            await StoreContextSeed.SeedAsync(storeContext);
            await IdentitySeed.SeedUsersAsync(scope.ServiceProvider, app.Environment);
        }
    }

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

public partial class Program;
