using System.Windows.Input;
using InventoryManager.Models;
using InventoryManager.Services;

namespace InventoryManager.ViewModels;

/// <summary>
/// Logic for the add/edit form. The same page and view model handle both: when a "productId"
/// query parameter is present the form edits that product, otherwise it adds a new one.
/// </summary>
[QueryProperty(nameof(ProductId), Routes.ProductIdParameter)]
public class ProductEditViewModel : BaseViewModel
{
    private readonly IProductRepository _repository;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;

    private int _productId;
    private Product? _existing;

    private string _name = string.Empty;
    private string? _category;
    private string _priceText = string.Empty;
    private string _quantityText = string.Empty;

    private string _nameError = string.Empty;
    private string _categoryError = string.Empty;
    private string _priceError = string.Empty;
    private string _quantityError = string.Empty;

    // Fields the user has interacted with. Inline errors are only shown for these,
    // so a brand-new empty form does not start out covered in red messages.
    private readonly HashSet<string> _touched = new();

    public ProductEditViewModel(IProductRepository repository, IDialogService dialogs, INavigationService navigation)
    {
        _repository = repository;
        _dialogs = dialogs;
        _navigation = navigation;

        SaveCommand = new Command(async () => await SaveAsync());
        CancelCommand = new Command(async () => await CancelAsync());
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public IReadOnlyList<string> Categories => ProductCategories.All;

    public bool IsEditMode => _productId > 0;
    public string PageTitle => IsEditMode ? "Edit Product" : "Add Product";
    public string SaveButtonText => IsEditMode ? "Save changes" : "Add product";

    /// <summary>Set by Shell when editing. Triggers loading of the existing product.</summary>
    public int ProductId
    {
        get => _productId;
        set
        {
            _productId = value;
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(SaveButtonText));
            _ = LoadAsync();
        }
    }

    // ---- Form fields (each setter marks the field as touched and re-validates) ----

    public string Name
    {
        get => _name;
        set { if (SetProperty(ref _name, value)) Touch(nameof(Product.Name)); }
    }

    public string? Category
    {
        get => _category;
        set { if (SetProperty(ref _category, value)) Touch(nameof(Product.Category)); }
    }

    public string PriceText
    {
        get => _priceText;
        set { if (SetProperty(ref _priceText, value)) Touch(nameof(Product.Price)); }
    }

    public string QuantityText
    {
        get => _quantityText;
        set { if (SetProperty(ref _quantityText, value)) Touch(nameof(Product.Quantity)); }
    }

    // ---- Inline error messages ----

    public string NameError
    {
        get => _nameError;
        private set { if (SetProperty(ref _nameError, value)) OnPropertyChanged(nameof(HasNameError)); }
    }

    public string CategoryError
    {
        get => _categoryError;
        private set { if (SetProperty(ref _categoryError, value)) OnPropertyChanged(nameof(HasCategoryError)); }
    }

    public string PriceError
    {
        get => _priceError;
        private set { if (SetProperty(ref _priceError, value)) OnPropertyChanged(nameof(HasPriceError)); }
    }

    public string QuantityError
    {
        get => _quantityError;
        private set { if (SetProperty(ref _quantityError, value)) OnPropertyChanged(nameof(HasQuantityError)); }
    }

    public bool HasNameError => NameError.Length > 0;
    public bool HasCategoryError => CategoryError.Length > 0;
    public bool HasPriceError => PriceError.Length > 0;
    public bool HasQuantityError => QuantityError.Length > 0;

    /// <summary>
    /// When editing, loads the product from SQLite and fills the form. Async so the read does not
    /// block the UI thread.
    /// </summary>
    private async Task LoadAsync()
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

            _existing = product;

            // Set the backing fields directly so filling the form does not count as "touched".
            _touched.Clear();
            _name = product.Name;
            _category = product.Category;
            _priceText = product.Price.ToString("0.00", System.Globalization.CultureInfo.CurrentCulture);
            _quantityText = product.Quantity.ToString();
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Category));
            OnPropertyChanged(nameof(PriceText));
            OnPropertyChanged(nameof(QuantityText));
            RefreshErrors();
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Error", $"Could not load the product.\n\nDetails: {ex.Message}");
        }
    }

    // ---- Validation ----

    private void Touch(string field)
    {
        _touched.Add(field);
        RefreshErrors();
    }

    /// <summary>
    /// Builds a Product from the text boxes and checks every rule.
    /// Text that cannot be parsed gives a parse error; the Product.Validate() rules
    /// (price &gt; 0, 2 decimals, quantity &gt;= 0, ...) are applied to the rest.
    /// </summary>
    private List<ValidationError> Validate(out Product candidate)
    {
        var errors = new List<ValidationError>();
        candidate = new Product
        {
            Name = Name.Trim(),
            Category = Category ?? string.Empty
        };

        bool priceOk = InputParser.TryParsePrice(PriceText, out decimal price, out string priceError);
        if (priceOk) candidate.Price = price;
        else errors.Add(new ValidationError(nameof(Product.Price), priceError));

        bool quantityOk = InputParser.TryParseQuantity(QuantityText, out int quantity, out string quantityError);
        if (quantityOk) candidate.Quantity = quantity;
        else errors.Add(new ValidationError(nameof(Product.Quantity), quantityError));

        foreach (var error in candidate.Validate())
        {
            // Skip the model's message for a field that already has a parse error.
            if (error.Field == nameof(Product.Price) && !priceOk) continue;
            if (error.Field == nameof(Product.Quantity) && !quantityOk) continue;
            errors.Add(error);
        }

        return errors;
    }

    private void RefreshErrors()
    {
        var errors = Validate(out _);

        string MessageFor(string field) =>
            _touched.Contains(field)
                ? errors.FirstOrDefault(e => e.Field == field)?.Message ?? string.Empty
                : string.Empty;

        NameError = MessageFor(nameof(Product.Name));
        CategoryError = MessageFor(nameof(Product.Category));
        PriceError = MessageFor(nameof(Product.Price));
        QuantityError = MessageFor(nameof(Product.Quantity));
    }

    // ---- Commands ----

    /// <summary>
    /// Validates, then saves to SQLite. Async because the database write must not block the UI.
    /// Invalid data is never saved: the user gets inline messages plus an error alert.
    /// On success a confirmation alert is shown and we navigate back.
    /// </summary>
    private async Task SaveAsync()
    {
        if (IsBusy) return;

        // Show inline errors for every field, even ones the user never touched.
        _touched.UnionWith(new[]
        {
            nameof(Product.Name), nameof(Product.Category), nameof(Product.Price), nameof(Product.Quantity)
        });
        RefreshErrors();

        var errors = Validate(out Product candidate);
        if (errors.Count > 0)
        {
            await _dialogs.ShowMessageAsync(
                "Please fix the form",
                "The product was not saved:\n- " + string.Join("\n- ", errors.Select(e => e.Message)));
            return;
        }

        IsBusy = true;
        try
        {
            Product toSave;
            if (IsEditMode && _existing is not null)
            {
                // Update the loaded product so Id and CreatedAt are preserved.
                toSave = _existing;
                toSave.Name = candidate.Name;
                toSave.Category = candidate.Category;
                toSave.Price = candidate.Price;
                toSave.Quantity = candidate.Quantity;
            }
            else
            {
                toSave = candidate;
            }

            await _repository.SaveAsync(toSave);

            await _dialogs.ShowMessageAsync(
                IsEditMode ? "Product updated" : "Product added",
                $"\"{toSave.Name}\" was saved successfully.");
            await _navigation.GoBackAsync();
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Error", $"Could not save the product. Please try again.\n\nDetails: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CancelAsync()
    {
        try
        {
            await _navigation.GoBackAsync();
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Error", ex.Message);
        }
    }
}
