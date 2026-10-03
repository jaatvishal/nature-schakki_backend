using System.Collections.Concurrent;
using Core.Entities;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public interface ICartWorkflow
{
    Task<ShoppingCart> GetAsync(string requestedId, int? userId, CancellationToken cancellationToken = default);
    Task<ShoppingCart> ReplaceAsync(ShoppingCart submitted, int? userId, CancellationToken cancellationToken = default);
    Task<ShoppingCart> ChangeItemAsync(int productId, int quantityKg, string mode, int? userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string requestedId, int? userId, CancellationToken cancellationToken = default);
    Task<ShoppingCart> RemoveItemAsync(int productId, int userId, CancellationToken cancellationToken = default);
}

public class CartWorkflow(
    ICartService storage,
    StoreContext context,
    IOptions<CartOptions> options) : ICartWorkflow
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public async Task<ShoppingCart> GetAsync(string requestedId, int? userId, CancellationToken cancellationToken = default)
    {
        var id = ResolveReadableId(requestedId, userId);
        return await storage.GetCartAsync(id) ?? new ShoppingCart { Id = id };
    }

    public async Task<ShoppingCart> ReplaceAsync(ShoppingCart submitted, int? userId, CancellationToken cancellationToken = default)
    {
        var id = ResolveWritableId(submitted.Id, userId);
        return await WithLock(id, async () =>
        {
            var normalized = submitted.Items
                .GroupBy(x => x.ProductId)
                .Select(x => (ProductId: x.Key, QuantityKg: x.Sum(i => i.Quantity)))
                .ToList();
            return await SaveNormalizedAsync(id, normalized, cancellationToken);
        });
    }

    public async Task<ShoppingCart> ChangeItemAsync(
        int productId, int quantityKg, string mode, int? userId, CancellationToken cancellationToken = default)
    {
        if (userId == null) throw new UnauthorizedException("Authentication is required.");
        ValidateQuantity(quantityKg);
        var id = userId.Value.ToString();
        return await WithLock(id, async () =>
        {
            var current = await storage.GetCartAsync(id) ?? new ShoppingCart { Id = id };
            var quantities = current.Items.ToDictionary(x => x.ProductId, x => x.Quantity);
            var existing = quantities.GetValueOrDefault(productId);
            var next = mode == "add" ? existing + quantityKg : quantityKg;
            ValidateQuantity(next);
            quantities[productId] = next;
            return await SaveNormalizedAsync(id, quantities.Select(x => (x.Key, x.Value)).ToList(), cancellationToken);
        });
    }

    public async Task DeleteAsync(string requestedId, int? userId, CancellationToken cancellationToken = default)
    {
        var id = ResolveWritableId(requestedId, userId);
        await WithLock(id, async () =>
        {
            await storage.DeleteCartAsync(id);
            return new ShoppingCart { Id = id };
        });
    }

    public async Task<ShoppingCart> RemoveItemAsync(int productId, int userId, CancellationToken cancellationToken = default)
    {
        var id = userId.ToString();
        return await WithLock(id, async () =>
        {
            var current = await storage.GetCartAsync(id) ?? new ShoppingCart { Id = id };
            var quantities = current.Items.Where(x => x.ProductId != productId)
                .Select(x => (x.ProductId, x.Quantity)).ToList();
            return await SaveNormalizedAsync(id, quantities, cancellationToken);
        });
    }

    private async Task<ShoppingCart> SaveNormalizedAsync(
        string id, List<(int ProductId, int QuantityKg)> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            await storage.DeleteCartAsync(id);
            return new ShoppingCart { Id = id };
        }

        var ids = items.Select(x => x.ProductId).ToList();
        var products = await context.Products.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (products.Count != ids.Distinct().Count())
            throw new BadRequestException("One or more products are unavailable.");

        var cart = new ShoppingCart { Id = id };
        foreach (var item in items)
        {
            ValidateQuantity(item.QuantityKg);
            var product = products[item.ProductId];
            if (!product.IsActive || product.IsArchived)
                throw new BadRequestException($"Product {product.Name} is unavailable.");
            if (product.QuantityInStock < item.QuantityKg)
                throw new BadRequestException($"Insufficient stock for {product.Name}.");

            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = item.QuantityKg,
                PictureUrl = product.PictureUrl,
                Brand = product.Brand,
                Type = product.Type
            });
        }

        return await storage.SetCartAsync(cart) ?? cart;
    }

    private void ValidateQuantity(int quantityKg)
    {
        var rules = options.Value;
        if (quantityKg < rules.MinimumQuantityKg || quantityKg > rules.MaximumQuantityKg ||
            rules.IncrementKg <= 0 || quantityKg % rules.IncrementKg != 0)
            throw new BadRequestException(
                $"Quantity must be between {rules.MinimumQuantityKg} and {rules.MaximumQuantityKg} kg in {rules.IncrementKg} kg increments.");
    }

    private static string ResolveReadableId(string requestedId, int? userId)
    {
        if (string.IsNullOrWhiteSpace(requestedId))
            throw new BadRequestException("Cart id is required.");
        if (userId == null)
        {
            if (int.TryParse(requestedId, out _))
                throw new UnauthorizedException("Authentication is required.");
            return requestedId;
        }
        if (requestedId == userId.Value.ToString() || !int.TryParse(requestedId, out _))
            return requestedId;
        throw new UnauthorizedException("You cannot access another customer's cart.");
    }

    private static string ResolveWritableId(string requestedId, int? userId)
    {
        if (userId != null) return userId.Value.ToString();
        if (string.IsNullOrWhiteSpace(requestedId) || int.TryParse(requestedId, out _))
            throw new UnauthorizedException("Authentication is required.");
        return requestedId;
    }

    private static async Task<T> WithLock<T>(string id, Func<Task<T>> action)
    {
        var gate = Locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try { return await action(); }
        finally { gate.Release(); }
    }
}
