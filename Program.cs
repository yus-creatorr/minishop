using MiniShop.Data;
using MiniShop.Repositories;
using MiniShop.Services;
using MiniShop.UI;

var db = new Database();

if (!db.TestConnection())
{
    return;
}

try
{
    db.Initialize();
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to initialize database: {ex.Message}");
    return;
}

var categoryRepo = new CategoryRepository(db);
var productRepo = new ProductRepository(db);
var stockService = new StockService(db);

var menu = new ConsoleMenu(db, categoryRepo, productRepo, stockService);
menu.Run();
