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

        RemoveNativeInputUnderlines();

        // Services: one shared instance for the whole app
        builder.Services.AddSingleton<IProductRepository, ProductDatabase>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        // View models: a fresh instance each time a page is opened
        builder.Services.AddTransient<ProductListViewModel>();
        builder.Services.AddTransient<ProductEditViewModel>();
        builder.Services.AddTransient<ProductDetailViewModel>();
        builder.Services.AddTransient<LowStockViewModel>();

        // Pages
        builder.Services.AddTransient<ProductListPage>();
        builder.Services.AddTransient<ProductEditPage>();
        builder.Services.AddTransient<ProductDetailPage>();
        builder.Services.AddTransient<LowStockPage>();

        return builder.Build();
    }

    /// <summary>
    /// Android draws an underline under Entry, Picker and SearchBar. The app wraps inputs in its own
    /// bordered box (the "InputField" style), so the native underline is made transparent.
    /// </summary>
    private static void RemoveNativeInputUnderlines()
    {
#if ANDROID
        var transparent = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);

        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline",
            (handler, _) => handler.PlatformView.BackgroundTintList = transparent);

        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline",
            (handler, _) => handler.PlatformView.BackgroundTintList = transparent);

        Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) =>
        {
            // "search_plate" is the AndroidX SearchView's inner text box that carries the underline.
            var context = handler.PlatformView.Context;
            int plateId = context?.Resources?.GetIdentifier("search_plate", "id", context.PackageName) ?? 0;
            if (plateId != 0)
                handler.PlatformView.FindViewById(plateId)?.SetBackgroundColor(Android.Graphics.Color.Transparent);
        });
#endif
    }
}
