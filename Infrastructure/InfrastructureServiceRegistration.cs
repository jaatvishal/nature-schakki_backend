using Core.Interfaces;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Options;
using Infrastructure.Services;
using Infrastructure.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration config, IHostEnvironment? env = null)
    {
        if (env?.IsEnvironment("Testing") == true)
        {
            services.AddDbContext<StoreContext>(options =>
                options.UseInMemoryDatabase("StoreTestDb"));
            services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseInMemoryDatabase("IdentityTestDb"));
        }
        else
        {
            services.AddDbContext<StoreContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

            services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
        }

        services.AddIdentityCore<Identity.AppUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<Identity.AppRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AppIdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRespository, ProductRepository>();
        services.AddScoped<IProductSearchService, SqlProductSearchService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPaymentService, StripePaymentService>();
        services.Configure<EmailOptions>(config.GetSection(EmailOptions.SectionName));
        services.Configure<EmailVerificationOptions>(config.GetSection(EmailVerificationOptions.SectionName));
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<IEmailService>(provider =>
        {
            var emailOptions = config.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new();
            return emailOptions.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase)
                ? ActivatorUtilities.CreateInstance<SmtpEmailService>(provider)
                : throw new InvalidOperationException($"Unsupported email provider '{emailOptions.Provider}'.");
        });
        services.AddScoped<IFileStorageService, LocalFileStorage>();
        services.AddScoped<IBasketService, BasketService>();

        var cacheProvider = config["CacheProvider"] ?? "Memory";
        if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var connString = config.GetConnectionString("Redis")
                    ?? throw new InvalidOperationException("Redis connection string is not configured.");
                return ConnectionMultiplexer.Connect(connString);
            });
            services.AddScoped<ICartService, RedisCartStorage>();
            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddScoped<ICartService, InMemoryCartStorage>();
            services.AddScoped<ICacheService, MemoryCacheService>();
        }

        services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

        return services;
    }
}
