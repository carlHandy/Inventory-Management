namespace InventoryManager.Services;

/// <summary>Lets view models navigate without referencing Shell directly.</summary>
public interface INavigationService
{
    /// <summary>Opens the product details page for the given product.</summary>
    Task GoToDetailsAsync(int productId);

    /// <summary>Opens the add/edit form. Pass 0 (or nothing) to add a new product.</summary>
    Task GoToEditAsync(int productId = 0);

    /// <summary>Switches to the Low Stock tab.</summary>
    Task GoToLowStockAsync();

    /// <summary>Goes back one page.</summary>
    Task GoBackAsync();
}

/// <summary>Shell-based implementation of <see cref="INavigationService"/>.</summary>
public class NavigationService : INavigationService
{
    /// <summary>Async because page transitions are animated and awaited.</summary>
    public Task GoToDetailsAsync(int productId)
        => Shell.Current.GoToAsync($"{Routes.ProductDetails}?{Routes.ProductIdParameter}={productId}");

    public Task GoToEditAsync(int productId = 0)
        => productId > 0
            ? Shell.Current.GoToAsync($"{Routes.ProductEdit}?{Routes.ProductIdParameter}={productId}")
            : Shell.Current.GoToAsync(Routes.ProductEdit);

    public Task GoToLowStockAsync() => Shell.Current.GoToAsync($"//{Routes.LowStockTab}");

    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
