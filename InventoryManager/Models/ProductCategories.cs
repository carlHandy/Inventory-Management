namespace InventoryManager.Models;

/// <summary>The fixed list of categories offered in the Picker on the add/edit form.</summary>
public static class ProductCategories
{
    public static readonly IReadOnlyList<string> All = new List<string>
    {
        "Electronics",
        "Clothing",
        "Food & Drink",
        "Home & Garden",
        "Office Supplies",
        "Toys & Games",
        "Other"
    };
}
