using InventoryManager.ViewModels;

namespace InventoryManager.Views;

public partial class ProductEditPage : ContentPage
{
    public ProductEditPage(ProductEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
