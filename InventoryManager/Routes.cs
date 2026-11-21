namespace InventoryManager;

/// <summary>Central place for the Shell route names (avoids typos in "magic strings").</summary>
public static class Routes
{
    public const string LowStockTab = "lowstock";
    public const string ProductDetails = "details";
    public const string ProductEdit = "edit";

    /// <summary>Name of the query parameter used to pass the product Id between pages.</summary>
    public const string ProductIdParameter = "productId";
}
