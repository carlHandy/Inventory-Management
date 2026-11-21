namespace InventoryManager.Models;

/// <summary>
/// One validation problem: which field it belongs to and the message to show the user.
/// </summary>
public record ValidationError(string Field, string Message);
