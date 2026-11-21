using InventoryManager.ViewModels;

namespace InventoryManager.Views;

public partial class LowStockPage : ContentPage
{
    private readonly LowStockViewModel _viewModel;

    public LowStockPage(LowStockViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>Refresh every time the tab is shown so stock changes appear immediately.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
