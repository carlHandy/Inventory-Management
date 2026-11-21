using System.Collections.ObjectModel;
using System.Windows.Input;
using InventoryManager.Models;
using InventoryManager.Services;

namespace InventoryManager.ViewModels;

/// <summary>Logic for the main Products list screen.</summary>
public class ProductListViewModel : BaseViewModel
{
    private readonly IProductRepository _repository;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;

    // Everything loaded from the database; Products is the filtered view of it.
    private List<Product> _allProducts = new();

    private string _searchText = string.Empty;
    private bool _inStockOnly;
    private int _lowStockCount;
    private Product? _selectedProduct;

    public ProductListViewModel(IProductRepository repository, IDialogService dialogs, INavigationService navigation)
    {
        _repository = repository;
        _dialogs = dialogs;
        _navigation = navigation;

        AddCommand = new Command(async () => await AddAsync());
    }

    /// <summary>The products currently shown (after search / filter).</summary>
    public ObservableCollection<Product> Products { get; } = new();

    public ICommand AddCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    /// <summary>When on, products with quantity 0 are hidden so only "available" products show.</summary>
    public bool InStockOnly
    {
        get => _inStockOnly;
        set
        {
            if (SetProperty(ref _inStockOnly, value))
                ApplyFilter();
        }
    }

    public int LowStockCount
    {
        get => _lowStockCount;
        private set
        {
            if (SetProperty(ref _lowStockCount, value))
            {
                OnPropertyChanged(nameof(HasLowStock));
                OnPropertyChanged(nameof(LowStockBanner));
            }
        }
    }

    public bool HasLowStock => LowStockCount > 0;

    public string LowStockBanner => LowStockCount == 1
        ? "1 product is low on stock (see the Low Stock tab)"
        : $"{LowStockCount} products are low on stock (see the Low Stock tab)";

    /// <summary>Bound to the CollectionView. Selecting an item opens its details page.</summary>
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
    /// Loads all products from SQLite. Async so the database read happens off the UI thread:
    /// the page stays responsive and shows the list when the data arrives.
    /// Called every time the page appears, so changes made on other pages are picked up.
    /// </summary>
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            _allProducts = await _repository.GetAllAsync();
            LowStockCount = _allProducts.Count(p => p.IsLowStock);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Error", $"Could not load the products. Please try again.\n\nDetails: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        var term = SearchText.Trim();

        IEnumerable<Product> query = _allProducts;
        if (InStockOnly)
            query = query.Where(p => !p.IsOutOfStock);
        if (term.Length > 0)
            query = query.Where(p =>
                p.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                p.Category.Contains(term, StringComparison.CurrentCultureIgnoreCase));

        Products.Clear();
        foreach (var product in query)
            Products.Add(product);
    }

    /// <summary>Async because navigation awaits the page transition.</summary>
    private async Task AddAsync()
    {
        try
        {
            await _navigation.GoToEditAsync();
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Error", $"Could not open the form. {ex.Message}");
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
            // Clear the highlight so the same product can be tapped again later.
            SelectedProduct = null;
        }
    }
}
