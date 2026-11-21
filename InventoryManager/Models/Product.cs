using System.Globalization;
using SQLite;

namespace InventoryManager.Models;

/// <summary>
/// A product in the inventory. This class is both the C# model and (through the sqlite-net
/// attributes) the definition of the "Products" table in the SQLite database.
/// </summary>
[Table("Products")]
public class Product
{
    // ---- Business rules (constants, so they are defined in exactly one place) ----

    /// <summary>A product is "low stock" when its quantity is at or below this number.</summary>
    public const int LowStockThreshold = 5;

    public const int MaxNameLength = 100;
    public const decimal MaxPrice = 1_000_000m;
    public const int MaxQuantity = 1_000_000;

    // ---- Database columns ----

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(MaxNameLength), NotNull]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50), NotNull]
    public string Category { get; set; } = string.Empty;

    /// <summary>Unit price. decimal avoids floating-point rounding errors with money.</summary>
    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // ---- Calculated members (NOT stored in the database, hence [Ignore]) ----

    /// <summary>True when stock is at or below <see cref="LowStockThreshold"/> (includes out of stock).</summary>
    [Ignore]
    public bool IsLowStock => Quantity <= LowStockThreshold;

    [Ignore]
    public bool IsOutOfStock => Quantity <= 0;

    /// <summary>Total value of the stock on hand (price x quantity).</summary>
    [Ignore]
    public decimal TotalValue => Price * Quantity;

    [Ignore]
    public string PriceDisplay => Price.ToString("C", CultureInfo.CurrentCulture);

    [Ignore]
    public string TotalValueDisplay => TotalValue.ToString("C", CultureInfo.CurrentCulture);

    [Ignore]
    public string StockStatus => IsOutOfStock ? "Out of stock" : IsLowStock ? "Low stock" : "In stock";

    [Ignore]
    public StockLevel StockLevel => IsOutOfStock ? StockLevel.OutOfStock : IsLowStock ? StockLevel.Low : StockLevel.InStock;

    /// <summary>Up to two letters from the name (e.g. "Wireless Mouse" -> "WM"), shown in the list avatar.</summary>
    [Ignore]
    public string Initials
    {
        get
        {
            var words = (Name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => char.IsLetterOrDigit(w[0]))
                .ToArray();
            return words.Length switch
            {
                0 => "?",
                1 => words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant(),
                _ => string.Concat(char.ToUpperInvariant(words[0][0]), char.ToUpperInvariant(words[1][0]))
            };
        }
    }

    // ---- Methods ----

    /// <summary>
    /// Checks every business rule and returns the list of problems.
    /// An empty list means the product is valid and may be saved.
    /// </summary>
    public List<ValidationError> Validate()
    {
        var errors = new List<ValidationError>();

        // Name: required, trimmed, max length
        var name = Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            errors.Add(new ValidationError(nameof(Name), "Name is required."));
        else if (name.Length > MaxNameLength)
            errors.Add(new ValidationError(nameof(Name), $"Name must be {MaxNameLength} characters or fewer."));

        // Category: required and must be one of the fixed categories
        if (string.IsNullOrWhiteSpace(Category) || !ProductCategories.All.Contains(Category))
            errors.Add(new ValidationError(nameof(Category), "Please choose a category."));

        // Price: greater than 0, at most 2 decimal places, sensible upper limit
        if (Price <= 0)
            errors.Add(new ValidationError(nameof(Price), "Price must be greater than 0."));
        else if (Price > MaxPrice)
            errors.Add(new ValidationError(nameof(Price), $"Price must not exceed {MaxPrice:N0}."));
        else if (decimal.Round(Price, 2) != Price)
            errors.Add(new ValidationError(nameof(Price), "Price can have at most 2 decimal places."));

        // Quantity: whole number (it is an int) and not negative
        if (Quantity < 0)
            errors.Add(new ValidationError(nameof(Quantity), "Quantity cannot be negative."));
        else if (Quantity > MaxQuantity)
            errors.Add(new ValidationError(nameof(Quantity), $"Quantity must not exceed {MaxQuantity:N0}."));

        return errors;
    }
}
