using Asp.Versioning;
using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace API.Controllers;

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
}

[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
[Route("api/v{version:apiVersion}/admin")]
[ApiController]
public class AdminController(
    StoreContext context,
    AppIdentityDbContext identityContext,
    UserManager<AppUser> userManager,
    IOrderService orderService,
    IInventoryService inventoryService,
    IReviewService reviewService,
    IAuditService auditService,
    IFileStorageService fileStorage) : ControllerBase
{
    private int AdminId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string? IpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard()
    {
        var today = DateTime.UtcNow.Date;
        var trendStart = today.AddDays(-29);
        var terminalFailures = new[] { OrderStatus.Cancelled, OrderStatus.Failed, OrderStatus.Refunded };

        var orderCounts = await context.Orders.AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Count = x.Count(), Revenue = x.Sum(o => o.Total) })
            .ToListAsync();

        var recentOrderEntities = await context.Orders.AsNoTracking()
            .Include(x => x.OrderItems)
            .Include(x => x.DeliveryMethod)
            .OrderByDescending(x => x.OrderDate)
            .Take(8)
            .ToListAsync();

        var lowStock = await context.Inventories.AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.QuantityOnHand - x.ReservedQuantity <= x.ReorderLevel)
            .OrderBy(x => x.QuantityOnHand - x.ReservedQuantity)
            .Take(8)
            .ToListAsync();

        var salesRows = await context.Orders.AsNoTracking()
            .Where(x => x.OrderDate >= trendStart && x.Status == OrderStatus.Delivered)
            .GroupBy(x => x.OrderDate.Date)
            .Select(x => new { Date = x.Key, Count = x.Count(), Value = x.Sum(o => o.Total) })
            .OrderBy(x => x.Date)
            .ToListAsync();
        var salesTrend = salesRows.Select(x => new ChartPointDto
        {
            Label = x.Date.ToString("yyyy-MM-dd"), Count = x.Count, Value = x.Value
        }).ToList();

        var topProducts = await context.OrderItems.AsNoTracking()
            .Where(x => x.Order != null && !terminalFailures.Contains(x.Order.Status))
            .GroupBy(x => new { x.ProductId, x.ProductName })
            .Select(x => new TopProductDto
            {
                ProductId = x.Key.ProductId,
                Name = x.Key.ProductName,
                Quantity = x.Sum(i => i.Quantity),
                Revenue = x.Sum(i => i.Price * i.Quantity)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(5)
            .ToListAsync();

        var customerRoleId = await identityContext.Roles.Where(x => x.Name == "Customer")
            .Select(x => x.Id).FirstOrDefaultAsync();
        var recentUsers = await userManager.Users.AsNoTracking()
            .Where(x => identityContext.UserRoles.Any(r => r.UserId == x.Id && r.RoleId == customerRoleId))
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToListAsync();

        return Ok(new AdminDashboardDto
        {
            UserCount = await userManager.Users.CountAsync(),
            ProductCount = await context.Products.CountAsync(x => !x.IsArchived),
            OrderCount = orderCounts.Sum(x => x.Count),
            PendingOrders = orderCounts.Where(x => x.Status is OrderStatus.Pending or OrderStatus.Processing or OrderStatus.Packed)
                .Sum(x => x.Count),
            CompletedOrders = orderCounts.Where(x => x.Status == OrderStatus.Delivered).Sum(x => x.Count),
            CancelledOrders = orderCounts.Where(x => x.Status == OrderStatus.Cancelled).Sum(x => x.Count),
            CodOrders = await context.Orders.CountAsync(x => x.PaymentMethod == "COD"),
            Revenue = orderCounts.Where(x => x.Status == OrderStatus.Delivered).Sum(x => x.Revenue),
            SuccessfulPayments = await context.Payments.CountAsync(x => x.Status == PaymentStatus.Succeeded),
            FailedPayments = await context.Payments.CountAsync(x => x.Status == PaymentStatus.Failed),
            PendingPayments = await context.Orders.CountAsync(x => x.PaymentMethod == "COD" && x.Status != OrderStatus.Delivered && !terminalFailures.Contains(x.Status)),
            LowStockCount = await context.Inventories.CountAsync(x => x.QuantityOnHand - x.ReservedQuantity <= x.ReorderLevel),
            OrdersByStatus = orderCounts.Select(x => new ChartPointDto
            {
                Label = x.Status.ToString(),
                Count = x.Count,
                Value = x.Revenue
            }).ToList(),
            RevenueTrend = salesTrend,
            TopProducts = topProducts,
            LowStockProducts = lowStock.Select(MapProduct).ToList(),
            RecentOrders = recentOrderEntities.Select(x => MapOrder(x, [])).ToList(),
            RecentRegistrations = recentUsers.Select(x => new AdminUserDto
            {
                Id = x.Id,
                Name = x.DisplayName,
                Email = x.Email ?? string.Empty,
                EmailVerified = x.EmailConfirmed,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                LastLoginAt = x.LastLoginAt
            }).ToList()
        });
    }

    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetUsers(
        string? search = null, bool? active = null, string sort = "newest", int page = 1, int pageSize = 20)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var customerRoleId = await identityContext.Roles.Where(x => x.Name == "Customer")
            .Select(x => x.Id).FirstOrDefaultAsync();
        var query = userManager.Users.AsNoTracking()
            .Where(x => identityContext.UserRoles.Any(r => r.UserId == x.Id && r.RoleId == customerRoleId));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Email!.Contains(search) || x.DisplayName.Contains(search));
        if (active.HasValue)
            query = query.Where(x => x.IsActive == active);

        query = sort switch
        {
            "name" => query.OrderBy(x => x.DisplayName),
            "email" => query.OrderBy(x => x.Email),
            "oldest" => query.OrderBy(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        var total = await query.CountAsync();
        var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var ids = users.Select(x => x.Id).ToList();
        var statRows = await context.Orders.AsNoTracking()
            .Where(x => ids.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count(), Spent = x.Sum(o => o.Status == OrderStatus.Delivered ? o.Total : 0) })
            .ToListAsync();
        var stats = statRows.ToDictionary(x => x.UserId, x => (x.Count, x.Spent));

        return Ok(new PagedResult<AdminUserDto>
        {
            Items = users.Select(x => MapUser(x, stats.GetValueOrDefault(x.Id))).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("users/{id:int}")]
    public async Task<ActionResult<AdminUserDto>> GetUser(int id)
    {
        var user = await userManager.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound();
        if (!await userManager.IsInRoleAsync(user, "Customer")) return NotFound();
        var statRow = await context.Orders.AsNoTracking().Where(x => x.UserId == id)
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count(), Spent = x.Sum(o => o.Status == OrderStatus.Delivered ? o.Total : 0) })
            .FirstOrDefaultAsync();
        return Ok(MapUser(user, statRow == null ? null : (statRow.Count, statRow.Spent)));
    }

    [HttpPut("users/{id:int}/status")]
    public async Task<IActionResult> SetUserStatus(int id, SetUserStatusDto dto)
    {
        if (id == AdminId) return BadRequest("Administrators cannot deactivate their own account.");
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        if (!await userManager.IsInRoleAsync(user, "Customer")) return BadRequest("Only customer accounts can be managed here.");
        user.IsActive = dto.IsActive;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest("Unable to update account status.");
        await auditService.LogAsync(AdminId, dto.IsActive ? "Activate user" : "Deactivate user",
            "User", id, null, IpAddress);
        return NoContent();
    }

    [HttpGet("users/{id:int}/orders")]
    public async Task<IActionResult> GetUserOrders(int id) =>
        Ok((await context.Orders.AsNoTracking().Include(x => x.OrderItems).Include(x => x.DeliveryMethod)
            .Where(x => x.UserId == id).OrderByDescending(x => x.OrderDate).ToListAsync())
            .Select(x => MapOrder(x, [])));

    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<AdminProductDto>>> GetProducts(
        string? search = null, int? categoryId = null, bool? active = null, bool includeArchived = false,
        string sort = "name", int page = 1, int pageSize = 20)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = context.Products.AsNoTracking().Include(x => x.Category).Include(x => x.Inventory).AsQueryable();
        if (!includeArchived) query = query.Where(x => !x.IsArchived);
        if (active.HasValue) query = query.Where(x => x.IsActive == active);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.Contains(search) || x.Sku.Contains(search) || x.Brand.Contains(search));
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId);
        query = sort switch
        {
            "price" => query.OrderBy(x => x.Price),
            "stock" => query.OrderBy(x => x.Inventory != null ? x.Inventory.QuantityOnHand : x.QuantityInStock),
            "newest" => query.OrderByDescending(x => x.CreatedAt),
            _ => query.OrderBy(x => x.Name)
        };
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResult<AdminProductDto>
        {
            Items = items.Select(MapProduct).ToList(), TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    [HttpPost("products")]
    public async Task<ActionResult<AdminProductDto>> CreateProduct(AdminProductUpsertDto dto)
    {
        if (await context.Products.AnyAsync(x => x.Sku == dto.Sku)) return Conflict("SKU already exists.");
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var category = await GetOrCreateCategory(dto.Category);
        var brand = await GetOrCreateBrand(dto.Brand);
        var product = new Product
        {
            Name = dto.Name.Trim(), Description = dto.Description.Trim(), Sku = dto.Sku.Trim(),
            Price = dto.Price, PictureUrl = dto.PictureUrl, Type = category.Name, Brand = brand.Name,
            QuantityInStock = dto.Stock, Unit = dto.Unit.Trim(), IsActive = dto.IsActive,
            CategoryId = category.Id, BrandId = brand.Id
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        context.Inventories.Add(new Inventory { ProductId = product.Id, QuantityOnHand = dto.Stock });
        context.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = product.Id, QuantityChange = dto.Stock, PreviousQuantity = 0,
            NewQuantity = dto.Stock, Reason = "Initial stock", ActorUserId = AdminId
        });
        await context.SaveChangesAsync();
        await auditService.LogAsync(AdminId, "Add product", "Product", product.Id, $"SKU {product.Sku}", IpAddress);
        if (transaction != null) await transaction.CommitAsync();
        return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, MapProduct(product));
    }

    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, AdminProductUpsertDto dto)
    {
        var product = await context.Products.Include(x => x.Inventory).FirstOrDefaultAsync(x => x.Id == id);
        if (product == null) return NotFound();
        if (await context.Products.AnyAsync(x => x.Id != id && x.Sku == dto.Sku)) return Conflict("SKU already exists.");
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var category = await GetOrCreateCategory(dto.Category);
        var brand = await GetOrCreateBrand(dto.Brand);
        var previousPrice = product.Price;
        var priceChanged = previousPrice != dto.Price;
        product.Name = dto.Name.Trim();
        product.Description = dto.Description.Trim();
        product.Sku = dto.Sku.Trim();
        product.Price = dto.Price;
        product.PictureUrl = dto.PictureUrl;
        product.Type = category.Name;
        product.Brand = brand.Name;
        product.Unit = dto.Unit.Trim();
        product.IsActive = dto.IsActive;
        product.CategoryId = category.Id;
        product.BrandId = brand.Id;
        product.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        if (product.Inventory == null)
        {
            context.Inventories.Add(new Inventory { ProductId = id, QuantityOnHand = dto.Stock });
            context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = id, QuantityChange = dto.Stock, PreviousQuantity = 0,
                NewQuantity = dto.Stock, Reason = "Inventory initialized during product edit", ActorUserId = AdminId
            });
            product.QuantityInStock = dto.Stock;
            await context.SaveChangesAsync();
        }
        else if (product.Inventory.QuantityOnHand != dto.Stock)
            await inventoryService.AdjustStockAsync(id, dto.Stock - product.Inventory.QuantityOnHand,
                "Product edit stock adjustment", AdminId);
        await auditService.LogAsync(AdminId, priceChanged ? "Change product price" : "Edit product",
            "Product", id, priceChanged ? $"Price {previousPrice} -> {dto.Price}" : null, IpAddress);
        if (transaction != null) await transaction.CommitAsync();
        return NoContent();
    }

    [HttpPut("products/{id:int}/active")]
    public async Task<IActionResult> SetProductActive(int id, [FromBody] bool active)
    {
        var product = await context.Products.FindAsync(id);
        if (product == null) return NotFound();
        product.IsActive = active;
        await context.SaveChangesAsync();
        await auditService.LogAsync(AdminId, active ? "Activate product" : "Deactivate product", "Product", id, null, IpAddress);
        return NoContent();
    }

    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> ArchiveProduct(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product == null) return NotFound();
        product.IsArchived = true;
        product.IsActive = false;
        await context.SaveChangesAsync();
        await auditService.LogAsync(AdminId, "Archive product", "Product", id, null, IpAddress);
        return NoContent();
    }

    [HttpPost("products/images")]
    [RequestSizeLimit(5_242_880)]
    public async Task<ActionResult<object>> UploadProductImage(IFormFile file)
    {
        if (file.Length == 0 || file.Length > 5_242_880) return BadRequest("Image must be between 1 byte and 5 MB.");
        await using var stream = file.OpenReadStream();
        var extension = await DetectImageExtension(stream);
        if (extension == null) return BadRequest("Only valid JPEG, PNG, or WebP images are allowed.");
        stream.Position = 0;
        var url = await fileStorage.SaveFileAsync(stream, $"product-{Guid.NewGuid():N}{extension}", $"image/{extension.TrimStart('.')}");
        await auditService.LogAsync(AdminId, "Upload product image", "ProductImage", null, null, IpAddress);
        var publicUrl = Uri.IsWellFormedUriString(url, UriKind.Absolute)
            ? url
            : $"{Request.Scheme}://{Request.Host}{url}";
        return Ok(new { url = publicUrl });
    }

    [HttpGet("categories")]
    public async Task<ActionResult<PagedResult<AdminCategoryDto>>> GetCategories(
        string? search = null, bool? active = null, int page = 1, int pageSize = 20)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = context.ProductCategories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Name.Contains(search));
        if (active.HasValue) query = query.Where(x => x.IsActive == active);
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminCategoryDto
            {
                Id = x.Id, Name = x.Name, Description = x.Description, IsActive = x.IsActive,
                ProductCount = x.Products.Count
            }).ToListAsync();
        return Ok(new PagedResult<AdminCategoryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    [HttpPost("categories")]
    public async Task<ActionResult<AdminCategoryDto>> CreateCategory(AdminCategoryUpsertDto dto)
    {
        if (await context.ProductCategories.AnyAsync(x => x.Name == dto.Name)) return Conflict("Category already exists.");
        var category = new ProductCategory { Name = dto.Name.Trim(), Description = dto.Description, IsActive = dto.IsActive };
        context.ProductCategories.Add(category);
        await context.SaveChangesAsync();
        await auditService.LogAsync(AdminId, "Add category", "Category", category.Id, null, IpAddress);
        return Ok(new AdminCategoryDto { Id = category.Id, Name = category.Name, Description = category.Description, IsActive = category.IsActive });
    }

    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, AdminCategoryUpsertDto dto)
    {
        var category = await context.ProductCategories.FindAsync(id);
        if (category == null) return NotFound();
        if (await context.ProductCategories.AnyAsync(x => x.Id != id && x.Name == dto.Name)) return Conflict("Category already exists.");
        category.Name = dto.Name.Trim();
        category.Description = dto.Description;
        category.IsActive = dto.IsActive;
        await context.SaveChangesAsync();
        await auditService.LogAsync(AdminId, "Edit category", "Category", id, null, IpAddress);
        return NoContent();
    }

    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<AdminOrderDto>>> GetOrders(
        string? search = null, OrderStatus? status = null, DateTime? from = null, DateTime? to = null,
        string sort = "newest", int page = 1, int pageSize = 20)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = context.Orders.AsNoTracking().Include(x => x.OrderItems).Include(x => x.DeliveryMethod).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.BuyerEmail.Contains(search) || x.Id.ToString().Contains(search));
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (from.HasValue) query = query.Where(x => x.OrderDate >= from.Value);
        if (to.HasValue) query = query.Where(x => x.OrderDate < to.Value.Date.AddDays(1));
        query = sort == "oldest" ? query.OrderBy(x => x.OrderDate) : query.OrderByDescending(x => x.OrderDate);
        var total = await query.CountAsync();
        var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var ids = orders.Select(x => x.Id).ToList();
        var histories = await context.OrderStatusHistories.AsNoTracking().Where(x => ids.Contains(x.OrderId))
            .OrderBy(x => x.CreatedAt).ToListAsync();
        return Ok(new PagedResult<AdminOrderDto>
        {
            Items = orders.Select(x => MapOrder(x, histories.Where(h => h.OrderId == x.Id))).ToList(),
            TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    [HttpGet("orders/{id:int}")]
    public async Task<ActionResult<AdminOrderDto>> GetOrder(int id)
    {
        var order = await context.Orders.AsNoTracking().Include(x => x.OrderItems).Include(x => x.DeliveryMethod)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();
        var history = await context.OrderStatusHistories.AsNoTracking().Where(x => x.OrderId == id)
            .OrderBy(x => x.CreatedAt).ToListAsync();
        return Ok(MapOrder(order, history));
    }

    [HttpPut("orders/{id:int}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, UpdateOrderStatusDto dto)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var current = await context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (current == null) return NotFound();
        if (current.Status == dto.Status) return NoContent();
        await orderService.UpdateOrderStatusAsync(id, dto.Status);
        context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = id, FromStatus = current.Status, ToStatus = dto.Status, ChangedByUserId = AdminId
        });
        await context.SaveChangesAsync();
        await auditService.LogAsync(AdminId, "Change order status", "Order", id,
            $"{current.Status} -> {dto.Status}", IpAddress);
        if (transaction != null) await transaction.CommitAsync();
        return NoContent();
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<PagedResult<AdminInventoryDto>>> GetInventory(
        string? search = null, bool lowStockOnly = false, int page = 1, int pageSize = 20)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = context.Inventories.AsNoTracking().Include(x => x.Product).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Product != null && (x.Product.Name.Contains(search) || x.Product.Sku.Contains(search)));
        if (lowStockOnly) query = query.Where(x => x.QuantityOnHand - x.ReservedQuantity <= x.ReorderLevel);
        var total = await query.CountAsync();
        var records = await query.OrderBy(x => x.Product!.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResult<AdminInventoryDto>
        {
            Items = records.Select(MapInventory).ToList(), TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    [HttpPut("inventory/{productId:int}")]
    public async Task<IActionResult> AdjustInventory(int productId, AdjustInventoryDto dto)
    {
        if (dto.QuantityChange == 0) return BadRequest("Quantity change cannot be zero.");
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var inventory = await context.Inventories.FirstOrDefaultAsync(x => x.ProductId == productId);
        if (inventory == null) return NotFound();
        if (inventory.QuantityOnHand != dto.ExpectedQuantity)
            return Conflict("Inventory changed since it was loaded. Refresh and try again.");
        if (dto.ReorderLevel.HasValue)
            inventory.ReorderLevel = dto.ReorderLevel.Value;
        await inventoryService.AdjustStockAsync(productId, dto.QuantityChange, dto.Reason.Trim(), AdminId);
        await auditService.LogAsync(AdminId, "Adjust stock", "Inventory", productId,
            $"Quantity change {dto.QuantityChange}; reason: {dto.Reason.Trim()}", IpAddress);
        if (transaction != null) await transaction.CommitAsync();
        return NoContent();
    }

    [HttpGet("inventory/{productId:int}/history")]
    public async Task<IActionResult> GetInventoryHistory(int productId) =>
        Ok(await context.InventoryTransactions.AsNoTracking().Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.CreatedAt).Take(100)
            .Select(x => new InventoryMovementDto
            {
                Id = x.Id, ProductId = x.ProductId, QuantityChange = x.QuantityChange,
                PreviousQuantity = x.PreviousQuantity, NewQuantity = x.NewQuantity,
                Reason = x.Reason, ActorUserId = x.ActorUserId, OrderId = x.OrderId, CreatedAt = x.CreatedAt
            }).ToListAsync());

    [HttpGet("payments")]
    public async Task<ActionResult<PagedResult<AdminPaymentDto>>> GetPayments(
        string? search = null, string? status = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 20)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = context.Orders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.BuyerEmail.Contains(search) || x.Id.ToString().Contains(search));
        if (from.HasValue) query = query.Where(x => x.OrderDate >= from.Value);
        if (to.HasValue) query = query.Where(x => x.OrderDate < to.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("Collected", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.PaymentMethod == "COD" && x.Status == OrderStatus.Delivered);
            else if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.PaymentMethod == "COD" && x.Status != OrderStatus.Delivered && x.Status != OrderStatus.Cancelled && x.Status != OrderStatus.Failed);
            else if (status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.Status == OrderStatus.Cancelled || x.Status == OrderStatus.Failed);
        }
        var total = await query.CountAsync();
        var orders = await query.OrderByDescending(x => x.OrderDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var ids = orders.Select(x => x.Id).ToList();
        var payments = await context.Payments.AsNoTracking().Where(x => ids.Contains(x.OrderId))
            .GroupBy(x => x.OrderId).Select(x => x.OrderByDescending(p => p.CreatedAt).First()).ToDictionaryAsync(x => x.OrderId);
        return Ok(new PagedResult<AdminPaymentDto>
        {
            Items = orders.Select(x => MapPayment(x, payments.GetValueOrDefault(x.Id))).ToList(),
            TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    [HttpGet("reports")]
    public async Task<ActionResult<AdminReportDto>> GetReports(DateTime? from = null, DateTime? to = null)
    {
        var start = (from ?? DateTime.UtcNow.Date.AddDays(-29)).Date;
        var end = (to ?? DateTime.UtcNow.Date).Date.AddDays(1);
        if (start >= end || end.Subtract(start).TotalDays > 366) return BadRequest("Date range must be between 1 and 366 days.");
        var query = context.Orders.AsNoTracking().Where(x => x.OrderDate >= start && x.OrderDate < end);
        var salesRows = await query.Where(x => x.Status == OrderStatus.Delivered)
            .GroupBy(x => x.OrderDate.Date)
            .Select(x => new { Date = x.Key, Count = x.Count(), Value = x.Sum(o => o.Total) })
            .OrderBy(x => x.Date).ToListAsync();
        var top = await context.OrderItems.AsNoTracking()
            .Where(x => x.Order != null && x.Order.OrderDate >= start && x.Order.OrderDate < end &&
                x.Order.Status != OrderStatus.Cancelled && x.Order.Status != OrderStatus.Failed &&
                x.Order.Status != OrderStatus.Refunded)
            .GroupBy(x => new { x.ProductId, x.ProductName })
            .Select(x => new TopProductDto
            {
                ProductId = x.Key.ProductId, Name = x.Key.ProductName,
                Quantity = x.Sum(i => i.Quantity), Revenue = x.Sum(i => i.Price * i.Quantity)
            }).OrderByDescending(x => x.Quantity).Take(10).ToListAsync();
        return Ok(new AdminReportDto
        {
            From = start, To = end.AddDays(-1),
            OrderCount = await query.CountAsync(),
            Revenue = await query.Where(x => x.Status == OrderStatus.Delivered).SumAsync(x => x.Total),
            CodOrders = await query.CountAsync(x => x.PaymentMethod == "COD"),
            CancelledOrders = await query.CountAsync(x => x.Status == OrderStatus.Cancelled),
            SalesTrend = salesRows.Select(x => new ChartPointDto
            {
                Label = x.Date.ToString("yyyy-MM-dd"), Count = x.Count, Value = x.Value
            }).ToList(),
            TopProducts = top
        });
    }

    [HttpGet("audit")]
    public async Task<ActionResult<PagedResult<AdminAuditDto>>> GetAuditLog(
        string? search = null, int page = 1, int pageSize = 20)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = context.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Action.Contains(search) || x.EntityType.Contains(search));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminAuditDto
            {
                Id = x.Id, UserId = x.UserId, Action = x.Action, EntityType = x.EntityType,
                EntityId = x.EntityId, Details = x.Details, CreatedAt = x.CreatedAt
            }).ToListAsync();
        return Ok(new PagedResult<AdminAuditDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<IReadOnlyList<AdminAlertDto>>> GetAlerts()
    {
        var stockAlerts = await context.Inventories.AsNoTracking().Include(x => x.Product)
            .Where(x => x.QuantityOnHand - x.ReservedQuantity <= x.ReorderLevel)
            .OrderBy(x => x.QuantityOnHand - x.ReservedQuantity).Take(20)
            .ToListAsync();
        var alerts = stockAlerts.Select(x => new AdminAlertDto
            {
                Severity = x.QuantityOnHand - x.ReservedQuantity <= 0 ? "Critical" : "Warning",
                Type = x.QuantityOnHand - x.ReservedQuantity <= 0 ? "OutOfStock" : "LowStock",
                Message = $"{x.Product!.Name} has {x.QuantityOnHand - x.ReservedQuantity} available",
                Link = "/admin/inventory"
            }).ToList();
        var pending = await context.Orders.CountAsync(
            x => x.Status == OrderStatus.Pending || x.Status == OrderStatus.Processing);
        if (pending > 0) alerts.Add(new AdminAlertDto
        {
            Severity = "Info", Type = "PendingOrders", Message = $"{pending} orders require attention", Link = "/admin/orders"
        });
        var since = DateTime.UtcNow.AddHours(-24);
        var newOrders = await context.Orders.CountAsync(x => x.OrderDate >= since);
        if (newOrders > 0) alerts.Add(new AdminAlertDto
        {
            Severity = "Info", Type = "NewOrders", Message = $"{newOrders} new orders in the last 24 hours", Link = "/admin/orders"
        });
        var cancellations = await context.Orders.CountAsync(x => x.Status == OrderStatus.Cancelled && x.UpdatedAt >= since);
        if (cancellations > 0) alerts.Add(new AdminAlertDto
        {
            Severity = "Warning", Type = "OrderCancellations", Message = $"{cancellations} recent order cancellations", Link = "/admin/orders"
        });
        var paymentIssues = await context.Payments.CountAsync(x => x.Status == PaymentStatus.Failed && x.UpdatedAt >= since);
        if (paymentIssues > 0) alerts.Add(new AdminAlertDto
        {
            Severity = "Critical", Type = "PaymentIssues", Message = $"{paymentIssues} recent payment failures", Link = "/admin/payments"
        });
        return Ok(alerts);
    }

    [HttpGet("coupons")]
    public async Task<IActionResult> GetCoupons() => Ok(await context.Coupons.AsNoTracking().ToListAsync());

    [HttpPost("coupons")]
    public async Task<IActionResult> CreateCoupon(CreateCouponDto dto)
    {
        if (!Enum.TryParse<CouponType>(dto.Type, true, out var type)) return BadRequest("Invalid coupon type.");
        var coupon = new Coupon
        {
            Code = dto.Code.Trim().ToUpperInvariant(), Type = type, Value = dto.Value,
            MinimumOrderAmount = dto.MinimumOrderAmount, MaxUsageCount = dto.MaxUsageCount,
            ExpiresAt = dto.ExpiresAt, IsActive = true
        };
        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();
        await auditService.LogAsync(AdminId, "Add coupon", "Coupon", coupon.Id, null, IpAddress);
        return Ok(coupon);
    }

    [HttpGet("reviews/pending")]
    public async Task<IActionResult> GetPendingReviews() =>
        Ok(await context.ProductReviews.AsNoTracking().Where(x => x.Status == ReviewStatus.Pending).ToListAsync());

    [HttpPut("reviews/{id:int}/moderate")]
    public async Task<IActionResult> ModerateReview(int id, [FromBody] ReviewStatus status)
    {
        var review = await reviewService.ModerateReviewAsync(id, status);
        await auditService.LogAsync(AdminId, "Moderate review", "Review", id, status.ToString(), IpAddress);
        return Ok(review);
    }

    private async Task<ProductCategory> GetOrCreateCategory(string name)
    {
        var normalized = name.Trim();
        var category = await context.ProductCategories.FirstOrDefaultAsync(x => x.Name == normalized);
        if (category != null) return category;
        category = new ProductCategory { Name = normalized };
        context.ProductCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private async Task<ProductBrand> GetOrCreateBrand(string name)
    {
        var normalized = name.Trim();
        var brand = await context.ProductBrands.FirstOrDefaultAsync(x => x.Name == normalized);
        if (brand != null) return brand;
        brand = new ProductBrand { Name = normalized };
        context.ProductBrands.Add(brand);
        await context.SaveChangesAsync();
        return brand;
    }

    private static AdminUserDto MapUser(AppUser user, (int Count, decimal Spent)? stats) => new()
    {
        Id = user.Id, Name = user.DisplayName, Email = user.Email ?? string.Empty,
        EmailVerified = user.EmailConfirmed, IsActive = user.IsActive, CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt, TotalOrders = stats?.Count ?? 0, TotalSpent = stats?.Spent ?? 0m
    };

    private static AdminProductDto MapProduct(Product product) => new()
    {
        Id = product.Id, Name = product.Name, Description = product.Description, Sku = product.Sku,
        Brand = product.Brand, Category = product.Category?.Name ?? product.Type, Price = product.Price,
        Stock = product.Inventory?.QuantityOnHand ?? product.QuantityInStock, Unit = product.Unit,
        PictureUrl = product.PictureUrl, IsActive = product.IsActive, IsArchived = product.IsArchived
    };

    private static AdminProductDto MapProduct(Inventory inventory)
    {
        if (inventory.Product == null) return new AdminProductDto();
        var product = MapProduct(inventory.Product);
        product.Stock = inventory.QuantityOnHand;
        return product;
    }

    private static AdminInventoryDto MapInventory(Inventory inventory) => new()
    {
        ProductId = inventory.ProductId, ProductName = inventory.Product?.Name ?? string.Empty,
        Sku = inventory.Product?.Sku ?? string.Empty, QuantityOnHand = inventory.QuantityOnHand,
        ReservedQuantity = inventory.ReservedQuantity, AvailableQuantity = inventory.AvailableQuantity,
        ReorderLevel = inventory.ReorderLevel, IsLowStock = inventory.AvailableQuantity <= inventory.ReorderLevel
    };

    private static AdminOrderDto MapOrder(Order order, IEnumerable<OrderStatusHistory> history) => new()
    {
        Id = order.Id, UserId = order.UserId, Customer = order.BuyerEmail, OrderDate = order.OrderDate,
        Status = order.Status.ToString(), PaymentMethod = order.PaymentMethod,
        PaymentStatus = order.PaymentMethod == "COD"
            ? order.Status == OrderStatus.Delivered ? "Collected"
                : order.Status is OrderStatus.Cancelled or OrderStatus.Failed ? "Failed" : "Pending"
            : "Pending",
        Subtotal = order.Subtotal, DeliveryCost = order.DeliveryCost, Discount = order.Discount, Total = order.Total,
        OrderItems = order.OrderItems.Select(x => new OrderItemDto
        {
            ProductId = x.ProductId, ProductName = x.ProductName, PictureUrl = x.PictureUrl,
            Price = x.Price, Quantity = x.Quantity
        }).ToList(),
        Timeline = history.Select(x => new OrderStatusHistoryDto
        {
            FromStatus = x.FromStatus.ToString(), ToStatus = x.ToStatus.ToString(),
            ChangedByUserId = x.ChangedByUserId, ChangedAt = x.CreatedAt
        }).ToList()
    };

    private static AdminPaymentDto MapPayment(Order order, Payment? payment) => new()
    {
        Id = payment?.Id, OrderId = order.Id, Customer = order.BuyerEmail, Method = order.PaymentMethod,
        Status = payment?.Status.ToString() ?? (order.Status == OrderStatus.Delivered ? "Collected"
            : order.Status is OrderStatus.Cancelled or OrderStatus.Failed ? "Failed" : "Pending"),
        Amount = payment?.Amount ?? order.Total, Reference = payment?.PaymentIntentId, Date = payment?.CreatedAt ?? order.OrderDate
    };

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));

    private static async Task<string?> DetectImageExtension(Stream stream)
    {
        var header = new byte[12];
        var read = await stream.ReadAsync(header);
        if (read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return ".png";
        if (read >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255)
            return ".jpg";
        if (read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return ".webp";
        return null;
    }
}
