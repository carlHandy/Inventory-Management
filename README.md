# Inventory Manager (.NET MAUI)

A small-business mobile app for registering products and monitoring available stock. Built with
.NET 10 MAUI (C#), plain MVVM, and a local SQLite database (sqlite-net-pcl).

## What it does

| Screen | Purpose |
|---|---|
| **Products** (tab 1) | List of all products from SQLite, search by name/category, "only in stock" switch, low-stock banner, **Add** toolbar button. Tap a product to open its details. |
| **Low Stock** (tab 2) | Automatically lists products with quantity <= 5, with a count summary. Refreshes every time it appears. |
| **Product Details** | All fields, low-stock badge, stock value, quick -1 / +1 stock adjust, **Edit**, **Delete** (with confirmation). |
| **Add / Edit Product** | One form for both modes: name, category (Picker), price, quantity, inline validation, Save / Cancel. |

## Build and run

Requirements: .NET 10 SDK with the MAUI workload, plus an Android SDK/emulator (or Xcode on macOS for iOS / Mac Catalyst, or Windows for the Windows target).

* **Visual Studio 2022/2026**: install the ".NET Multi-platform App UI development" workload, open `InventoryManager.sln`, choose a device/emulator and press F5.
* **Command line**:
  ```
  dotnet workload install maui
  cd InventoryManager
  dotnet build -t:Run -f net10.0-android
  ```
  (On Linux only the Android target can be built; if restore complains about the iOS workload, add `-p:TargetFrameworks=net10.0-android`.)

Packages: `Microsoft.Maui.Controls`, `sqlite-net-pcl`, `SQLitePCLRaw.bundle_green`. No other libraries.

## Project structure

```
InventoryManager.sln
InventoryManager/
  InventoryManager.csproj
  App.xaml(.cs), AppShell.xaml(.cs), MauiProgram.cs, Routes.cs
  Models/       Product.cs, ProductCategories.cs, InputParser.cs, ValidationError.cs
  Services/     IProductRepository.cs, ProductDatabase.cs (SQLite),
                IDialogService.cs (alerts), INavigationService.cs (Shell navigation)
  ViewModels/   BaseViewModel.cs, ProductListViewModel.cs, ProductEditViewModel.cs,
                ProductDetailViewModel.cs, LowStockViewModel.cs
  Views/        ProductListPage, ProductEditPage, ProductDetailPage, LowStockPage (.xaml + .xaml.cs)
  Resources/    AppIcon/, Splash/, Styles/Colors.xaml, Styles/Styles.xaml
  Platforms/    Android, iOS, MacCatalyst, Windows (standard MAUI boilerplate)
```

Architecture: **View (XAML) -> ViewModel -> IProductRepository -> SQLite**. Pages contain no data
logic (they only set the BindingContext and refresh on `OnAppearing`). View models never touch
SQLite; they call `IProductRepository`. Everything is registered in `MauiProgram.cs` (dependency injection).

## How the mandatory requirements are met

**A. UI and navigation**
* 4 screens (`Views/`), Shell with a bottom tab bar (`AppShell.xaml`) and registered routes for Details and Edit (`AppShell.xaml.cs`, `Routes.cs`).
* Product Id is passed as a query parameter (`details?productId=3`) and received with `[QueryProperty]` in `ProductDetailViewModel` / `ProductEditViewModel`.
* Layouts/controls: Grid, VerticalStackLayout, ScrollView, CollectionView, Border cards, SearchBar, Switch, Picker, Entry (numeric keyboard), Button, ToolbarItem. Shared look via `Resources/Styles/*.xaml`, light and dark theme aware.

**B. Models and structure**
* `Models/Product.cs`: typed properties (int, string, decimal, DateTime), constants, calculated members `IsLowStock`, `IsOutOfStock`, `TotalValue`, `StockStatus`, and `Validate()` returning a list of errors.
* Data code is isolated in `Services/ProductDatabase.cs`; UI code in `Views/`; logic in `ViewModels/`.

**C. SQLite**
* `ProductDatabase` uses `SQLiteAsyncConnection` on `FileSystem.AppDataDirectory/inventory.db3`; table created lazily on first use.
* Full CRUD: `SaveAsync` (create + update), `GetAllAsync`, `GetByIdAsync`, `GetLowStockAsync`, `DeleteAsync`.
* Data persists because it is stored in a file; every page reloads from the DB in `OnAppearing`.

**D. Async** - see the section below.

**E. Validation and errors**
* Rules in `Product.Validate()` + `InputParser`; inline messages on the form; an alert lists the problems and nothing is saved when invalid.
* Confirmation alerts after add, update and delete; a status message after stock adjusts.
* Every database call in the view models is inside try/catch and shows a friendly error alert.

## Scenario items

| Item | Where |
|---|---|
| a) Register products | `ProductEditPage` / `ProductEditViewModel.SaveAsync` (add mode) |
| b) List available products | `ProductListPage` + `ProductListViewModel` ("only in stock" switch hides quantity 0) |
| c) View details | `ProductDetailPage` + `ProductDetailViewModel` |
| d) Update info and stock | Edit mode of the form; +1 / -1 buttons (`AdjustStockAsync`) on details |
| e) Delete | Delete button on details, `DeleteAsync` with confirmation |
| f) SQLite storage | `Services/ProductDatabase.cs` |
| g) Validation | `Product.Validate()`, `InputParser`, `ProductEditViewModel` |
| Extra) Low stock | `Product.IsLowStock` / `LowStockThreshold = 5`, `GetLowStockAsync`, Low Stock tab, main-list banner and badges |

Validation rules: name required, trimmed, max 100 chars; category required (from fixed list); price numeric, > 0, at most 2 decimals, <= 1,000,000; quantity a whole number, >= 0, <= 1,000,000.

## Asynchronous programming

Reading and writing the SQLite file is disk I/O. If it ran on the UI thread the screen would freeze,
so every database method is `async`/`await` (sqlite-net's `...Async` methods run the work on a
background thread and the UI thread stays free to draw and respond to touches). `await` resumes on the
UI thread afterwards, so updating bound properties is safe. Each method has an XML doc comment stating its purpose too.

| Method | Why it is async |
|---|---|
| `ProductDatabase.GetConnectionAsync` | Opens the DB and creates the table (disk I/O) once, lazily; a `SemaphoreSlim` stops two callers initialising at the same time. |
| `ProductDatabase.GetAllAsync / GetByIdAsync / GetLowStockAsync` | Query reads from disk without blocking the UI. |
| `ProductDatabase.SaveAsync` | Insert/update writes to disk without freezing the form. |
| `ProductDatabase.DeleteAsync` | Delete writes to disk without freezing the screen. |
| `ProductListViewModel.LoadAsync`, `LowStockViewModel.LoadAsync` | Load lists when the page appears; `IsBusy` prevents overlapping loads. |
| `ProductDetailViewModel.LoadAsync` | Loads one product by Id. |
| `ProductDetailViewModel.DeleteAsync` | Waits for the user's confirmation, then the delete, then navigates back. |
| `ProductDetailViewModel.AdjustStockAsync` | Saves the new quantity to SQLite. |
| `ProductEditViewModel.LoadAsync` / `SaveAsync` | Load product to edit; validate then write to SQLite. |
| `DialogService.ShowMessageAsync / ConfirmAsync` | An alert stays open until the user responds; we must wait for the answer. |
| `NavigationService.*Async` | Page transitions are animated and awaited. |
| `OnAppearing` overrides (`async void`) | The only legal use of `async void`: an event-style override that cannot return a Task. Errors are caught inside `LoadAsync`. |

## Notes
* Low-stock threshold is the constant `Product.LowStockThreshold` (5).
* On first launch (when the database file is created) 12 sample products are inserted from `Services/SeedData.cs`, including low-stock and out-of-stock items. They are not re-added later, so deleted products stay deleted.
* Only the Android target was compiled during development on Linux; iOS / Mac Catalyst / Windows projects use the standard template configuration.
