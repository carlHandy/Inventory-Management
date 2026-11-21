using InventoryManager.Models;

namespace InventoryManager.Services;

/// <summary>
/// Sample products inserted the very first time the database is created, so the app has
/// realistic data to show straight away. It includes in-stock, low-stock (at or below
/// <see cref="Product.LowStockThreshold"/>) and out-of-stock items to demonstrate the low-stock feature.
/// </summary>
public static class SeedData
{
    public static List<Product> GetProducts() => new()
    {
        new Product { Name = "Wireless Mouse",          Category = "Electronics",     Price = 24.99m,  Quantity = 42 },
        new Product { Name = "USB-C Charging Cable",    Category = "Electronics",     Price = 9.50m,   Quantity = 3 },
        new Product { Name = "Bluetooth Speaker",       Category = "Electronics",     Price = 59.00m,  Quantity = 0 },
        new Product { Name = "Cotton T-Shirt",          Category = "Clothing",        Price = 15.00m,  Quantity = 60 },
        new Product { Name = "Denim Jacket",            Category = "Clothing",        Price = 74.95m,  Quantity = 4 },
        new Product { Name = "Bottled Water (24 pack)", Category = "Food & Drink",    Price = 6.99m,   Quantity = 35 },
        new Product { Name = "Ground Coffee 500g",      Category = "Food & Drink",    Price = 12.49m,  Quantity = 5 },
        new Product { Name = "Ceramic Plant Pot",       Category = "Home & Garden",   Price = 18.00m,  Quantity = 22 },
        new Product { Name = "LED Desk Lamp",           Category = "Home & Garden",   Price = 32.50m,  Quantity = 1 },
        new Product { Name = "A4 Printer Paper",        Category = "Office Supplies", Price = 7.25m,   Quantity = 80 },
        new Product { Name = "Ballpoint Pens (10)",     Category = "Office Supplies", Price = 4.99m,   Quantity = 0 },
        new Product { Name = "Jigsaw Puzzle 1000pc",    Category = "Toys & Games",    Price = 19.99m,  Quantity = 12 },
    };
}
