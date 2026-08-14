namespace Core.Interfaces;

public interface IInventoryService
{
    Task<bool> ReserveStockAsync(int productId, int quantity);
    Task ReleaseStockAsync(int productId, int quantity);
    Task<bool> HasAvailableStockAsync(int productId, int quantity);
    Task AdjustStockAsync(int productId, int quantity);
}
