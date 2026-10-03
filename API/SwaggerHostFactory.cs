using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

namespace API;

public static class SwaggerConfiguration
{
    public static void Configure(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "NaturesChakki API",
            Version = "v1"
        });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
        });
    }

    public static IApiVersioningBuilder AddVersionedApiExplorer(this IServiceCollection services) =>
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddMvc().AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
}

// Convention used by Swashbuckle CLI. It avoids executing Program startup,
// database connections, migrations, seeds, or Production configuration checks.
public class SwaggerHostFactory
{
    public static IHost CreateHost()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Swagger";
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(Program).Assembly);
        builder.Services.AddVersionedApiExplorer();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(SwaggerConfiguration.Configure);
        return builder.Build();
    }
}
