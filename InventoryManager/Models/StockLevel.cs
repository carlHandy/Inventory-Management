namespace InventoryManager.Models;

/// <summary>Stock state of a product, used to colour status pills (green / amber / red).</summary>
public enum StockLevel
{
    InStock,
    Low,
    OutOfStock
}
