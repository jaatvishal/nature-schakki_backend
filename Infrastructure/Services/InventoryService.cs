using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class InventoryService(StoreContext context) : IInventoryService
{
    public async Task<bool> ReserveStockAsync(int productId, int quantity)
    {
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(x => x.ProductId == productId);

        if (inventory == null)
        {
            var product = await context.Products.FindAsync(productId);
            if (product == null) return false;
            inventory = new Inventory { ProductId = productId, QuantityOnHand = product.QuantityInStock };
            context.Inventories.Add(inventory);
        }

        if (inventory.AvailableQuantity < quantity) return false;

        inventory.ReservedQuantity += quantity;
        inventory.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task ReleaseStockAsync(int productId, int quantity)
    {
        var inventory = await context.Inventories.FirstOrDefaultAsync(x => x.ProductId == productId);
        if (inventory == null) return;

        inventory.ReservedQuantity = Math.Max(0, inventory.ReservedQuantity - quantity);
        inventory.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<bool> HasAvailableStockAsync(int productId, int quantity)
    {
        var inventory = await context.Inventories.FirstOrDefaultAsync(x => x.ProductId == productId);
        if (inventory != null)
            return inventory.AvailableQuantity >= quantity;

        var product = await context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == productId);
        return product != null && product.QuantityInStock >= quantity;
    }

    public async Task AdjustStockAsync(
        int productId,
        int quantity,
        string reason = "Stock adjustment",
        int? actorUserId = null,
        int? orderId = null)
    {
        var inventory = await context.Inventories.FirstOrDefaultAsync(x => x.ProductId == productId);
        var product = await context.Products.FindAsync(productId)
            ?? throw new NotFoundException($"Product {productId} not found.");

        if (inventory == null)
        {
            inventory = new Inventory { ProductId = productId, QuantityOnHand = product.QuantityInStock };
            context.Inventories.Add(inventory);
        }

        var previousQuantity = inventory.QuantityOnHand;
        if (previousQuantity + quantity < 0)
            throw new BadRequestException($"Insufficient stock for product {productId}.");

        inventory.QuantityOnHand += quantity;
        product.QuantityInStock = inventory.QuantityOnHand;
        inventory.UpdatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        context.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = productId,
            QuantityChange = quantity,
            PreviousQuantity = previousQuantity,
            NewQuantity = inventory.QuantityOnHand,
            Reason = reason,
            ActorUserId = actorUserId,
            OrderId = orderId
        });
        await context.SaveChangesAsync();
    }
}
