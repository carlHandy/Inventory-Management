using System.Collections.ObjectModel;
using InventoryManager.Models;
using InventoryManager.Services;

namespace InventoryManager.ViewModels;

/// <summary>Logic for the Low Stock tab: lists products at or below the threshold.</summary>
public class LowStockViewModel : BaseViewModel
{
    private readonly IProductRepository _repository;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;

    private Product? _selectedProduct;
    private string _summary = string.Empty;

    public LowStockViewModel(IProductRepository repository, IDialogService dialogs, INavigationService navigation)
    {
        _repository = repository;
        _dialogs = dialogs;
        _navigation = navigation;
        Summary = ThresholdText;
    }

    public ObservableCollection<Product> Products { get; } = new();

    public string ThresholdText => $"Products with {Product.LowStockThreshold} or fewer items in stock.";

    /// <summary>Count summary shown at the top, e.g. "3 products need restocking".</summary>
    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public bool IsEmpty => Products.Count == 0;

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value) && value is not null)
                _ = OpenAsync(value);
        }
    }

    /// <summary>
    /// Loads only the low-stock products from SQLite. Async so the query runs off the UI thread.
    /// Called every time the tab appears so the list is always up to date.
    /// </summary>
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var lowStock = await _repository.GetLowStockAsync();

            Products.Clear();
            foreach (var product in lowStock)
                Products.Add(product);

            Summary = lowStock.Count switch
            {
                0 => "All products are well stocked.",
                1 => "1 product needs restocking",
                _ => $"{lowStock.Count} products need restocking"
            };
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Error", $"Could not load low-stock products.\n\nDetails: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenAsync(Product product)
    {
        try
        {
            await _navigation.GoToDetailsAsync(product.Id);
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Error", $"Could not open the product. {ex.Message}");
        }
        finally
        {
            SelectedProduct = null;
        }
    }
}
