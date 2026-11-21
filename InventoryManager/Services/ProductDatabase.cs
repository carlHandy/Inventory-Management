using InventoryManager.Models;
using SQLite;

namespace InventoryManager.Services;

/// <summary>
/// SQLite implementation of <see cref="IProductRepository"/>.
/// The database file lives in the app's private data folder, so it survives closing and
/// re-opening the app. Exceptions are deliberately NOT caught here; the view models catch
/// them and show a friendly message to the user.
/// </summary>
public class ProductDatabase : IProductRepository
{
    private const string DatabaseFileName = "inventory.db3";

    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache;

    private static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

    private SQLiteAsyncConnection? _connection;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Lazy, thread-safe initialisation: opens the database and creates the Products table the
    /// first time any data method is called. It is async because creating the table is disk I/O
    /// that must not block the UI thread (which would freeze the app at start-up).
    /// The semaphore makes sure two pages loading at once cannot initialise it twice.
    /// </summary>
    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        await _initLock.WaitAsync();
        try
        {
            if (_connection is null)
            {
                // Check before opening: SQLiteOpenFlags.Create makes the file on first open.
                bool isFirstRun = !File.Exists(DatabasePath);

                var connection = new SQLiteAsyncConnection(DatabasePath, Flags);
                await connection.CreateTableAsync<Product>();

                // Seed sample data only when the database is brand new, so products the user
                // deletes do not reappear the next time the app starts.
                if (isFirstRun)
                    await connection.InsertAllAsync(SeedData.GetProducts());

                _connection = connection;
            }
        }
        finally
        {
            _initLock.Release();
        }

        return _connection;
    }

    /// <summary>Reads all products from disk without blocking the UI thread.</summary>
    public async Task<List<Product>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Product>().OrderBy(p => p.Name).ToListAsync();
    }

    /// <summary>Reads a single product by primary key without blocking the UI thread.</summary>
    public async Task<Product?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Product>().Where(p => p.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>Queries only the low-stock rows (the filter runs inside SQLite, off the UI thread).</summary>
    public async Task<List<Product>> GetLowStockAsync()
    {
        var db = await GetConnectionAsync();
        int threshold = Product.LowStockThreshold;
        return await db.Table<Product>()
                       .Where(p => p.Quantity <= threshold)
                       .OrderBy(p => p.Quantity)
                       .ToListAsync();
    }

    /// <summary>
    /// Insert (new product, Id == 0) or update (existing product). Writing to disk is async so the
    /// UI stays responsive while saving. UpdatedAt is stamped here so every save is tracked.
    /// </summary>
    public async Task SaveAsync(Product product)
    {
        var db = await GetConnectionAsync();
        product.Name = product.Name.Trim();
        product.UpdatedAt = DateTime.Now;

        if (product.Id == 0)
        {
            product.CreatedAt = DateTime.Now;
            await db.InsertAsync(product);   // sqlite-net fills in product.Id
        }
        else
        {
            await db.UpdateAsync(product);
        }
    }

    /// <summary>Deletes the row from disk without blocking the UI thread.</summary>
    public async Task DeleteAsync(Product product)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(product);
    }
}
