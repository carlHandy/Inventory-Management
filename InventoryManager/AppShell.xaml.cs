using InventoryManager.Views;

namespace InventoryManager;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pages that are pushed on top of the tabs are registered as routes.
        Routing.RegisterRoute(Routes.ProductEdit, typeof(ProductEditPage));
    }
}
