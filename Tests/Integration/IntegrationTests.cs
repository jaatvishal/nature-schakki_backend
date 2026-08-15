using Core.Entities;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}

public class IntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public IntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        SeedData().GetAwaiter().GetResult();
    }

    private async Task SeedData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreContext>();
        var identityContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        await context.Database.EnsureCreatedAsync();
        await identityContext.Database.EnsureCreatedAsync();

        if (!await roleManager.RoleExistsAsync("Customer"))
            await roleManager.CreateAsync(new AppRole { Name = "Customer" });
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new AppRole { Name = "Admin" });

        if (!context.Products.Any())
        {
            context.ProductCategories.Add(new ProductCategory { Name = "Flour" });
            context.ProductBrands.Add(new ProductBrand { Name = "NaturesChakki" });
            await context.SaveChangesAsync();

            context.Products.Add(new Product
            {
                Name = "Test Flour",
                Description = "Test",
                Price = 50,
                PictureUrl = "/test.png",
                Type = "Flour",
                Brand = "NaturesChakki",
                QuantityInStock = 100,
                CategoryId = 1,
                BrandId = 1
            });
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetProducts_ReturnsOkWithPagination()
    {
        var response = await _client.GetAsync("/api/product?pageIndex=1&pageSize=6");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("data", out _));
        Assert.True(doc.RootElement.TryGetProperty("count", out _));
    }

    [Fact]
    public async Task GetProductById_ReturnsProduct()
    {
        var response = await _client.GetAsync("/api/product/1");
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(product);
        Assert.Equal("Test Flour", product!.Name);
    }

    [Fact]
    public async Task GetBrands_ReturnsStringArray()
    {
        var response = await _client.GetAsync("/api/product/brands");
        response.EnsureSuccessStatusCode();
        var brands = await response.Content.ReadFromJsonAsync<string[]>();
        Assert.NotNull(brands);
        Assert.Contains("NaturesChakki", brands!);
    }

    [Fact]
    public async Task GetTypes_ReturnsStringArray()
    {
        var response = await _client.GetAsync("/api/product/types");
        response.EnsureSuccessStatusCode();
        var types = await response.Content.ReadFromJsonAsync<string[]>();
        Assert.NotNull(types);
        Assert.Contains("Flour", types!);
    }

    [Fact]
    public async Task Register_ReturnsUserWithToken()
    {
        var email = $"user{Guid.NewGuid():N}@test.com";
        var dto = new Core.DTOs.RegisterDto
        {
            Email = email,
            DisplayName = "Test User",
            Password = "Password1!"
        };
        var response = await _client.PostAsJsonAsync("/api/v1/account/register", dto);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrWhiteSpace(token.GetString()));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/account/login", new
        {
            email = "nonexistent@test.com",
            password = "Wrong@123"
        });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCart_ReturnsEmptyCartForNewId()
    {
        var response = await _client.GetAsync("/api/v1/cart?id=newcart123");
        response.EnsureSuccessStatusCode();
        var cart = await response.Content.ReadFromJsonAsync<ShoppingCart>();
        Assert.NotNull(cart);
        Assert.Equal("newcart123", cart!.Id);
    }

    [Fact]
    public async Task UpdateCart_PersistsItems()
    {
        var cart = new ShoppingCart
        {
            Id = $"cart{Guid.NewGuid():N}",
            Items = [new CartItem { ProductId = 1, ProductName = "Test", Price = 50, Quantity = 2, PictureUrl = "/t.png", Brand = "NaturesChakki", Type = "Flour" }]
        };

        var postResponse = await _client.PostAsJsonAsync("/api/v1/cart", cart);
        postResponse.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync($"/api/v1/cart?id={cart.Id}");
        getResponse.EnsureSuccessStatusCode();
        var retrieved = await getResponse.Content.ReadFromJsonAsync<ShoppingCart>();
        Assert.Single(retrieved!.Items);
        Assert.Equal(2, retrieved.Items[0].Quantity);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetDeliveryMethods_ReturnsList()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreContext>();
        if (!context.DeliveryMethods.Any())
        {
            context.DeliveryMethods.Add(new DeliveryMethod
            {
                ShortName = "STD", Description = "Standard", Price = 5, DeliveryTimeDays = 3
            });
            context.SaveChanges();
        }

        var response = await _client.GetAsync("/api/v1/deliverymethods");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AdminDashboard_RequiresAdminRole()
    {
        var response = await _client.GetAsync("/api/v1/admin/dashboard");
        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetNotifications_RequiresAuth()
    {
        var response = await _client.GetAsync("/api/v1/notifications");
        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SearchProducts_WithBrandFilter_ReturnsFilteredResults()
    {
        var response = await _client.GetAsync("/api/product?brands=NaturesChakki&pageIndex=1&pageSize=10");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("count").GetInt32() >= 1);
    }
}
