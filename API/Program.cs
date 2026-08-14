using API.Middleware;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddControllers();

builder.Services.AddDbContext<StoreContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IProductRespository, ProductRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>),typeof(GenericRepository<>));
builder.Services.AddCors();
//builder.Services.AddSingleton<IConnectionMultiplexer>(config =>
//{
//    var connString = builder.Configuration.GetConnectionString("Redis")
//    ?? throw new Exception("Connnot get redis connection string");
//    if (connString == null) throw new Exception("Redis connection string is not configured.");
//    var configuration = ConfigurationOptions.Parse(connString, true);
//    return ConnectionMultiplexer.Connect(configuration);
//});
builder.Services.AddScoped<ICartService, CartService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }

app.UseHttpsRedirection();

// app.UseAuthorization();
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(x=>x.AllowAnyHeader().AllowAnyMethod().
WithOrigins("http://localhost:5001", "https://localhost:5001","http://localhost:4200"));

app.MapControllers();

try
{
   using var scope = app.Services.CreateScope();
   var context = scope.ServiceProvider.GetRequiredService<StoreContext>(); 
   await context.Database.MigrateAsync();
   await StoreContextSeed.SeedAsync(context);
}
catch(SystemException ex)
{
   Console.WriteLine(ex.Message);
   throw; 
}

app.Run();
