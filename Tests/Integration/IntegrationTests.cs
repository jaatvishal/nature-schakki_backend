using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Identity;
using Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.RemoveAll<IFileStorageService>();
            services.AddSingleton<TestEmailService>();
            services.AddSingleton<IEmailService>(x => x.GetRequiredService<TestEmailService>());
            services.AddSingleton<IFileStorageService, TestFileStorageService>();
            services.Configure<EmailVerificationOptions>(options =>
            {
                options.OtpLifetimeMinutes = 10;
                options.MaxAttempts = 3;
                options.ResendCooldownSeconds = 1;
            });
        });
    }
}

public class TestFileStorageService : IFileStorageService
{
    public Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType) =>
        Task.FromResult($"/uploads/{Path.GetFileName(fileName)}");

    public Task DeleteFileAsync(string filePath) => Task.CompletedTask;
}

public class TestEmailService : IEmailService
{
    private readonly Dictionary<string, string> _messages = [];
    public bool FailSending { get; set; }

    public Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (FailSending)
            throw new Core.Exceptions.ServiceUnavailableException(
                "Verification email is temporarily unavailable. Please try again.");
        _messages[to] = body;
        return Task.CompletedTask;
    }

    public string GetOtp(string email) =>
        Regex.Match(_messages[email], @"\b\d{6}\b").Value;
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

        if (!context.DeliveryMethods.Any())
        {
            context.DeliveryMethods.Add(new DeliveryMethod
            {
                ShortName = "Standard Delivery",
                Description = "Standard local delivery",
                Price = 0,
                DeliveryTimeDays = 5
            });
            await context.SaveChangesAsync();
        }
    }

    private async Task<(int userId, string token)> CreateVerifiedUserAsync()
    {
        var email = $"order{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/v1/account/register", new
        {
            email,
            firstName = "Order",
            lastName = "Customer",
            password = "Password1!"
        });
        var otp = _factory.Services.GetRequiredService<TestEmailService>().GetOtp(email);
        var response = await _client.PostAsJsonAsync("/api/v1/account/verify-email", new { email, otp });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("userId").GetInt32(), json.GetProperty("token").GetString()!);
    }

    private async Task<(int userId, string token)> CreateAdminAsync()
    {
        var email = $"admin{Guid.NewGuid():N}@test.com";
        const string password = "Password1!";
        using (var scope = _factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var admin = new AppUser
            {
                Email = email, UserName = email, DisplayName = "Test Admin",
                EmailConfirmed = true, IsActive = true
            };
            var result = await manager.CreateAsync(admin, password);
            Assert.True(result.Succeeded);
            await manager.AddToRoleAsync(admin, "Admin");
        }

        var login = await _client.PostAsJsonAsync("/api/v1/account/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("userId").GetInt32(), json.GetProperty("token").GetString()!);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string url,
        string token,
        object? content = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (content != null)
            request.Content = JsonContent.Create(content);
        return await _client.SendAsync(request);
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
    public async Task Register_RequiresOtpBeforeLogin_ThenActivatesAccount()
    {
        var email = $"user{Guid.NewGuid():N}@test.com";
        var dto = new Core.DTOs.RegisterDto
        {
            Email = email,
            FirstName = "Test",
            LastName = "User",
            Password = "Password1!"
        };
        var response = await _client.PostAsJsonAsync("/api/v1/account/register", dto);

        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);

        var unverifiedLogin = await _client.PostAsJsonAsync("/api/v1/account/login", new
        {
            email,
            password = dto.Password
        });
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, unverifiedLogin.StatusCode);

        var emailService = _factory.Services.GetRequiredService<TestEmailService>();
        var verify = await _client.PostAsJsonAsync("/api/v1/account/verify-email", new
        {
            email,
            otp = emailService.GetOtp(email)
        });
        verify.EnsureSuccessStatusCode();
        var json = await verify.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidOrExpiredOtp_IsRejected()
    {
        var email = $"invalid{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/v1/account/register", new
        {
            email,
            firstName = "Invalid",
            lastName = "Otp",
            password = "Password1!"
        });

        var invalid = await _client.PostAsJsonAsync("/api/v1/account/verify-email", new
        {
            email,
            otp = "000000"
        });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>()
            .FindByEmailAsync(email);
        var otp = await context.EmailVerificationOtps.SingleAsync(x => x.UserId == user!.Id);
        otp.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var emailService = _factory.Services.GetRequiredService<TestEmailService>();
        var expired = await _client.PostAsJsonAsync("/api/v1/account/verify-email", new
        {
            email,
            otp = emailService.GetOtp(email)
        });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, expired.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_StopsAfterMaximumFailedAttempts()
    {
        var email = $"attempts{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/v1/account/register", new
        {
            email,
            firstName = "Maximum",
            lastName = "Attempts",
            password = "Password1!"
        });

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/account/verify-email", new
            {
                email,
                otp = "000000"
            });
            Assert.Equal(
                attempt == 3
                    ? System.Net.HttpStatusCode.TooManyRequests
                    : System.Net.HttpStatusCode.BadRequest,
                response.StatusCode);
        }
    }

    [Fact]
    public async Task ResendVerification_EnforcesCooldown_AndInvalidatesPreviousOtp()
    {
        var email = $"resend{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/v1/account/register", new
        {
            email,
            firstName = "Resend",
            lastName = "Otp",
            password = "Password1!"
        });
        var emailService = _factory.Services.GetRequiredService<TestEmailService>();
        var oldOtp = emailService.GetOtp(email);

        var limited = await _client.PostAsJsonAsync("/api/v1/account/resend-verification", new { email });
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, limited.StatusCode);

        await Task.Delay(1100);
        var resent = await _client.PostAsJsonAsync("/api/v1/account/resend-verification", new { email });
        resent.EnsureSuccessStatusCode();

        var oldCodeAttempt = await _client.PostAsJsonAsync("/api/v1/account/verify-email", new
        {
            email,
            otp = oldOtp
        });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, oldCodeAttempt.StatusCode);
    }

    [Fact]
    public async Task Register_WhenEmailProviderFails_ReturnsSafeServiceUnavailableResponse()
    {
        var emailService = _factory.Services.GetRequiredService<TestEmailService>();
        emailService.FailSending = true;
        try
        {
            var response = await _client.PostAsJsonAsync("/api/v1/account/register", new
            {
                email = $"emailfail{Guid.NewGuid():N}@test.com",
                firstName = "Email",
                lastName = "Failure",
                password = "Password1!"
            });

            Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("SMTP", body, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            emailService.FailSending = false;
        }
    }

    [Fact]
    public async Task CodCheckout_UsesDatabasePrice_UpdatesStock_ClearsCart_AndProtectsOrder()
    {
        var customer = await CreateVerifiedUserAsync();
        var otherCustomer = await CreateVerifiedUserAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreContext>();
        var product = await context.Products.FirstAsync();
        var originalStock = product.QuantityInStock;
        var deliveryId = await context.DeliveryMethods.Select(x => x.Id).FirstAsync();

        await _client.PostAsJsonAsync("/api/v1/cart", new ShoppingCart
        {
            Id = customer.userId.ToString(),
            Items =
            [
                new CartItem
                {
                    ProductId = product.Id,
                    ProductName = "Tampered",
                    Price = 1,
                    Quantity = 2,
                    PictureUrl = "/fake.png",
                    Brand = "Fake",
                    Type = "Fake"
                }
            ]
        });

        var checkout = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/orders", customer.token, new
        {
            deliveryMethodId = deliveryId,
            paymentMethod = "COD",
            shipToAddress = new
            {
                firstName = "Order",
                lastName = "Customer",
                street = "1 Test Street",
                city = "Test City",
                state = "Test State",
                zipCode = "123456",
                country = "India"
            }
        });
        checkout.EnsureSuccessStatusCode();
        var order = await checkout.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = order.GetProperty("id").GetInt32();
        Assert.Equal(product.Price, order.GetProperty("orderItems")[0].GetProperty("price").GetDecimal());
        Assert.Equal("COD", order.GetProperty("paymentMethod").GetString());

        context.ChangeTracker.Clear();
        Assert.Equal(originalStock - 2, (await context.Products.FindAsync(product.Id))!.QuantityInStock);

        var cart = await _client.GetFromJsonAsync<ShoppingCart>(
            $"/api/v1/cart?id={customer.userId}");
        Assert.Empty(cart!.Items);

        var forbiddenOrder = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/v1/orders/{orderId}", otherCustomer.token);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, forbiddenOrder.StatusCode);

        var ownOrders = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/orders", customer.token);
        var ownOrderList = await ownOrders.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(ownOrderList.EnumerateArray(), x => x.GetProperty("id").GetInt32() == orderId);

        var otherOrders = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/orders", otherCustomer.token);
        var otherOrderList = await otherOrders.Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(otherOrderList.EnumerateArray(), x => x.GetProperty("id").GetInt32() == orderId);
    }

    [Fact]
    public async Task CodCheckout_WithInvalidStock_DoesNotCreatePartialOrder()
    {
        var customer = await CreateVerifiedUserAsync();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreContext>();
        var product = await context.Products.FirstAsync();
        var beforeOrders = await context.Orders.CountAsync();
        var deliveryId = await context.DeliveryMethods.Select(x => x.Id).FirstAsync();

        await _client.PostAsJsonAsync("/api/v1/cart", new ShoppingCart
        {
            Id = customer.userId.ToString(),
            Items =
            [
                new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = product.QuantityInStock + 1,
                    PictureUrl = product.PictureUrl,
                    Brand = product.Brand,
                    Type = product.Type
                }
            ]
        });

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/orders", customer.token, new
        {
            deliveryMethodId = deliveryId,
            paymentMethod = "COD",
            shipToAddress = new
            {
                firstName = "Order",
                lastName = "Customer",
                street = "1 Test Street",
                city = "Test City",
                state = "Test State",
                zipCode = "123456",
                country = "India"
            }
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(beforeOrders, await context.Orders.CountAsync());
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
    public async Task Customer_CannotAccessAdminApi_WhileAdminCan()
    {
        var customer = await CreateVerifiedUserAsync();
        var customerResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/dashboard", customer.token);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, customerResponse.StatusCode);

        var admin = await CreateAdminAsync();
        var adminResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/dashboard", admin.token);
        adminResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Admin_ProductImageInventoryAndAuditFlow_Works()
    {
        var admin = await CreateAdminAsync();
        var sku = $"SKU-{Guid.NewGuid():N}";
        var create = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/admin/products", admin.token, new
        {
            name = "Admin Test Flour",
            description = "Created through admin API",
            sku,
            brand = "NaturesChakki",
            category = "Flour",
            price = 125m,
            stock = 5,
            unit = "1 kg",
            pictureUrl = "/images/placeholder.png",
            isActive = true
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, create.StatusCode);
        var product = await create.Content.ReadFromJsonAsync<JsonElement>();
        var productId = product.GetProperty("id").GetInt32();

        var update = await SendAuthorizedAsync(HttpMethod.Put, $"/api/v1/admin/products/{productId}", admin.token, new
        {
            name = "Updated Admin Flour", description = "Updated through admin API", sku,
            brand = "NaturesChakki", category = "Flour", price = 130m, stock = 5,
            unit = "1 kg", pictureUrl = "/images/placeholder.png", isActive = true
        });
        Assert.Equal(System.Net.HttpStatusCode.NoContent, update.StatusCode);

        Assert.Equal(System.Net.HttpStatusCode.NoContent,
            (await SendAuthorizedAsync(HttpMethod.Put, $"/api/v1/admin/products/{productId}/active", admin.token, false)).StatusCode);

        using var upload = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/products/images");
        upload.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin.token);
        var bytes = new byte[] { 255, 216, 255, 224, 0, 16, 74, 70, 73, 70 };
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        upload.Content = new MultipartFormDataContent { { file, "file", "test.jpg" } };
        var uploadResponse = await _client.SendAsync(upload);
        uploadResponse.EnsureSuccessStatusCode();

        var badUpload = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/products/images");
        badUpload.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin.token);
        var badFile = new ByteArrayContent("not an image"u8.ToArray());
        badFile.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        badUpload.Content = new MultipartFormDataContent { { badFile, "file", "fake.jpg" } };
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, (await _client.SendAsync(badUpload)).StatusCode);

        var inventoryResponse = await SendAuthorizedAsync(HttpMethod.Get,
            "/api/v1/admin/inventory?search=Updated", admin.token);
        var inventoryPage = await inventoryResponse.Content.ReadFromJsonAsync<JsonElement>();
        var inventory = inventoryPage.GetProperty("items")[0];
        var adjust = await SendAuthorizedAsync(HttpMethod.Put,
            $"/api/v1/admin/inventory/{productId}", admin.token, new
            {
                quantityChange = 3,
                reason = "Integration test restock",
                expectedQuantity = inventory.GetProperty("quantityOnHand").GetInt32()
            });
        Assert.Equal(System.Net.HttpStatusCode.NoContent, adjust.StatusCode);

        var history = await SendAuthorizedAsync(HttpMethod.Get,
            $"/api/v1/admin/inventory/{productId}/history", admin.token);
        var movements = await history.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(movements.EnumerateArray(), x => x.GetProperty("reason").GetString() == "Integration test restock");

        var audit = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/audit", admin.token);
        var auditPage = await audit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(auditPage.GetProperty("items").EnumerateArray(),
            x => x.GetProperty("entityType").GetString() == "Product");

        Assert.Equal(System.Net.HttpStatusCode.NoContent,
            (await SendAuthorizedAsync(HttpMethod.Delete, $"/api/v1/admin/products/{productId}", admin.token)).StatusCode);
    }

    [Fact]
    public async Task Admin_OrderStatusUpdate_RecordsHistory_AndRejectsInvalidTransition()
    {
        var admin = await CreateAdminAsync();
        var customer = await CreateVerifiedUserAsync();
        int orderId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<StoreContext>();
            var product = await context.Products.FirstAsync();
            var delivery = await context.DeliveryMethods.FirstAsync();
            var order = new Order
            {
                UserId = customer.userId, BuyerEmail = "status@test.com",
                ShipToAddress = new Address
                {
                    UserId = customer.userId, FirstName = "Status", LastName = "Test",
                    Street = "1 Test St", City = "City", State = "State", ZipCode = "123456", Country = "India"
                },
                DeliveryMethodId = delivery.Id, Status = Core.Enums.OrderStatus.Processing,
                PaymentMethod = "COD", Subtotal = product.Price, Total = product.Price,
                OrderItems =
                [
                    new OrderItem
                    {
                        ProductId = product.Id, ProductName = product.Name,
                        PictureUrl = product.PictureUrl, Price = product.Price, Quantity = 1
                    }
                ]
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            orderId = order.Id;
        }

        var packed = await SendAuthorizedAsync(HttpMethod.Put,
            $"/api/v1/admin/orders/{orderId}/status", admin.token, new { status = "Packed" });
        Assert.Equal(System.Net.HttpStatusCode.NoContent, packed.StatusCode);

        var invalid = await SendAuthorizedAsync(HttpMethod.Put,
            $"/api/v1/admin/orders/{orderId}/status", admin.token, new { status = "Delivered" });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);

        var detail = await SendAuthorizedAsync(HttpMethod.Get,
            $"/api/v1/admin/orders/{orderId}", admin.token);
        var json = await detail.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(json.GetProperty("timeline").EnumerateArray(),
            x => x.GetProperty("toStatus").GetString() == "Packed");
    }

    [Fact]
    public async Task Admin_UserAndPaymentHistory_DoNotExposeSensitiveFields()
    {
        var admin = await CreateAdminAsync();
        var customer = await CreateVerifiedUserAsync();
        var users = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/users", admin.token);
        users.EnsureSuccessStatusCode();
        var body = await users.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"passwordHash\":", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"otp\":", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"refreshToken\":", body, StringComparison.OrdinalIgnoreCase);

        var payments = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/admin/payments", admin.token);
        payments.EnsureSuccessStatusCode();

        var deactivate = await SendAuthorizedAsync(HttpMethod.Put,
            $"/api/v1/admin/users/{customer.userId}/status", admin.token, new { isActive = false });
        Assert.Equal(System.Net.HttpStatusCode.NoContent, deactivate.StatusCode);
        var blocked = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/orders", customer.token);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, blocked.StatusCode);
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
