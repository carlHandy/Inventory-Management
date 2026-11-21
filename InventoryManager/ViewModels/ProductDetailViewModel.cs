using System.Windows.Input;
using InventoryManager.Models;
using InventoryManager.Services;

namespace InventoryManager.ViewModels;

/// <summary>
/// Logic for the product details screen. The product Id arrives as the "productId" query
/// parameter from Shell navigation.
/// </summary>
[QueryProperty(nameof(ProductId), Routes.ProductIdParameter)]
public class ProductDetailViewModel : BaseViewModel
{
    private readonly IProductRepository _repository;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;

    private int _productId;
    private Product? _product;
    private string _statusMessage = string.Empty;

    public ProductDetailViewModel(IProductRepository repository, IDialogService dialogs, INavigationService navigation)
    {
        _repository = repository;
        _dialogs = dialogs;
        _navigation = navigation;

        EditCommand = new Command(async () => await EditAsync());
        DeleteCommand = new Command(async () => await DeleteAsync());
        AdjustStockCommand = new Command<string>(async delta => await AdjustStockAsync(delta));
    }

    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand AdjustStockCommand { get; }

    /// <summary>Set by Shell when the page is opened (from the query string).</summary>
    public int ProductId
    {
        get => _productId;
        set
        {
            _productId = value;
            _ = LoadAsync();
        }
    }

    // ---- Values shown on the page ----
    public string Name => _product?.Name ?? string.Empty;
    public string Category => _product?.Category ?? string.Empty;
    public string PriceDisplay => _product?.PriceDisplay ?? string.Empty;
    public string QuantityDisplay => _product?.Quantity.ToString() ?? string.Empty;
    public string TotalValueDisplay => _product?.TotalValueDisplay ?? string.Empty;
    public string StockStatus => _product?.StockStatus ?? string.Empty;
    public bool IsLowStock => _product?.IsLowStock ?? false;
    public string CreatedText => _product is null ? string.Empty : $"Added: {_product.CreatedAt:g}";
    public string UpdatedText => _product is null ? string.Empty : $"Last updated: {_product.UpdatedAt:g}";

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
                OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    /// <summary>
    /// Reads the product from SQLite. Async so the read does not block the UI thread. Called when
    /// the Id is received and again when the page re-appears (e.g. after returning from Edit).
    /// </summary>
    public async Task LoadAsync()
    {
        if (_productId <= 0) return;

        try
        {
            var product = await _repository.GetByIdAsync(_productId);
            if (product is null)
            {
                await _dialogs.ShowMessageAsync("Not found", "This product no longer exists.");
                await _navigation.GoBackAsync();
                return;
            }

            _product = product;
            OnPropertyChanged(string.Empty);   // empty name = "refresh every bound property"
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Error", $"Could not load the product.\n\nDetails: {ex.Message}");
        }
    }

    private async Task EditAsync()
    {
        try
        {
            await _navigation.GoToEditAsync(_productId);
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Error", $"Could not open the edit form. {ex.Message}");
        }
    }

    /// <summary>
    /// Asks for confirmation, then deletes. Async because we await the user's answer and then
    /// the database delete, without freezing the screen.
    /// </summary>
    private async Task DeleteAsync()
    {
        if (_product is null || IsBusy) return;

        bool confirmed = await _dialogs.ConfirmAsync(
            "Delete product",
            $"Are you sure you want to delete \"{_product.Name}\"? This cannot be undone.",
            "Delete", "Cancel");
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            string name = _product.Name;
            await _repository.DeleteAsync(_product);
            await _dialogs.ShowMessageAsync("Deleted", $"\"{name}\" was deleted.");
            await _navigation.GoBackAsync();
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Error", $"Could not delete the product. Please try again.\n\nDetails: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Quick stock adjustment (+1 / -1). Async because the new quantity is written to SQLite.
    /// Stock can never go below 0 or above the maximum.
    /// </summary>
    private async Task AdjustStockAsync(string? deltaText)
    {
        if (_product is null || IsBusy) return;
        if (!int.TryParse(deltaText, out int delta)) return;

        int newQuantity = _product.Quantity + delta;
        if (newQuantity < 0)
        {
            await _dialogs.ShowMessageAsync("Invalid quantity", "Stock cannot go below 0.");
            return;
        }
        if (newQuantity > Product.MaxQuantity)
        {
            await _dialogs.ShowMessageAsync("Invalid quantity", $"Stock cannot exceed {Product.MaxQuantity:N0}.");
            return;
        }

        IsBusy = true;
        int oldQuantity = _product.Quantity;
        try
        {
            _product.Quantity = newQuantity;
            await _repository.SaveAsync(_product);
            StatusMessage = $"Stock updated to {newQuantity}.";
            OnPropertyChanged(string.Empty);
        }
        catch (Exception ex)
        {
            _product.Quantity = oldQuantity;   // undo the in-memory change
            await _dialogs.ShowMessageAsync("Error", $"Could not update the stock. Please try again.\n\nDetails: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
