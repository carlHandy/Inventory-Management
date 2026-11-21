using System.Globalization;

namespace InventoryManager.Models;

/// <summary>
/// Converts the raw text typed into the form into numbers.
/// Keeping this here (not in the page) means the parsing rules can be tested and reused.
/// </summary>
public static class InputParser
{
    /// <summary>Parses a price such as "12.50". Currency symbols and thousands separators are rejected.</summary>
    public static bool TryParsePrice(string? text, out decimal price, out string error)
    {
        price = 0;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Price is required.";
            return false;
        }

        const NumberStyles styles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;
        if (!decimal.TryParse(text.Trim(), styles, CultureInfo.CurrentCulture, out price))
        {
            error = "Price must be a number, for example 12.50.";
            return false;
        }

        return true;
    }

    /// <summary>Parses a whole-number quantity such as "25". Decimals such as "2.5" are rejected.</summary>
    public static bool TryParseQuantity(string? text, out int quantity, out string error)
    {
        quantity = 0;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Quantity is required.";
            return false;
        }

        if (!int.TryParse(text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out quantity))
        {
            error = "Quantity must be a whole number, for example 25.";
            return false;
        }

        return true;
    }
}
