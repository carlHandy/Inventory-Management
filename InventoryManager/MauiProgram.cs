using Microsoft.Extensions.DependencyInjection;
using InventoryManager.Services;
using InventoryManager.ViewModels;
using InventoryManager.Views;

namespace InventoryManager;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Services: one shared instance for the whole app
        builder.Services.AddSingleton<IProductRepository, ProductDatabase>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        // View models: a fresh instance each time a page is opened
        builder.Services.AddTransient<ProductListViewModel>();

        // Pages
        builder.Services.AddTransient<ProductListPage>();

        return builder.Build();
    }
}
