using InventoryManager.Models;

namespace InventoryManager.Services;

/// <summary>
/// Everything the view models need from the data layer. View models depend on this interface,
/// never on SQLite directly, so UI code stays separate from data-management code.
/// </summary>
public interface IProductRepository
{
    /// <summary>Gets every product, ordered by name.</summary>
    Task<List<Product>> GetAllAsync();

    /// <summary>Gets one product, or null if no product has that Id.</summary>
    Task<Product?> GetByIdAsync(int id);

    /// <summary>Gets products at or below the low-stock threshold, lowest quantity first.</summary>
    Task<List<Product>> GetLowStockAsync();

    /// <summary>Inserts a new product (Id == 0) or updates an existing one.</summary>
    Task SaveAsync(Product product);

    /// <summary>Deletes a product.</summary>
    Task DeleteAsync(Product product);
}
