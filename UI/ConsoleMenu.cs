using System.Data;
using MiniShop.Data;
using MiniShop.Models;
using MiniShop.Repositories;
using MiniShop.Services;
using Npgsql;

namespace MiniShop.UI;

public class ConsoleMenu
{
    private readonly Database _db;
    private readonly CategoryRepository _categoryRepo;
    private readonly ProductRepository _productRepo;
    private readonly StockService _stockService;

    public ConsoleMenu(
        Database db,
        CategoryRepository categoryRepo,
        ProductRepository productRepo,
        StockService stockService)
    {
        _db = db;
        _categoryRepo = categoryRepo;
        _productRepo = productRepo;
        _stockService = stockService;
    }

    public void Run()
    {
        while (true)
        {
            PrintMenu();
            Console.Write("Choose an option: ");
            string choice = Console.ReadLine()?.Trim() ?? "";

            try
            {
                switch (choice)
                {
                    case "1": ListCategories(); break;
                    case "2": FindCategoryById(); break;
                    case "3": AddCategory(); break;
                    case "4": UpdateCategory(); break;
                    case "5": DeleteCategory(); break;

                    case "6": ListProducts(); break;
                    case "7": FindProductById(); break;
                    case "8": AddProduct(); break;
                    case "9": UpdateProduct(); break;
                    case "10": DeleteProduct(); break;

                    case "11": SearchProducts(); break;
                    case "12": FilterProducts(); break;

                    case "13": RegisterSale(); break;
                    case "14": RestockProduct(); break;

                    case "15": _productRepo.PrintProductsWithCategory(); break;
                    case "16": PrintLowStockReport(); break;
                    case "17": _productRepo.PrintCategorySummary(); break;
                    case "18": _productRepo.PrintSalesPerProduct(); break;

                    case "19": RaiseCategoryPricesWithDataSet(); break;

                    case "0":
                        Console.WriteLine("Goodbye!");
                        return;

                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private void PrintMenu()
    {
        Console.WriteLine("=== MiniShop Menu === ");
        Console.WriteLine(" 1. List categories");
        Console.WriteLine(" 2. Find category by id");
        Console.WriteLine(" 3. Add category");
        Console.WriteLine(" 4. Update category");
        Console.WriteLine(" 5. Delete category");
        Console.WriteLine(" 6. List products");
        Console.WriteLine(" 7. Find product by id");
        Console.WriteLine(" 8. Add product");
        Console.WriteLine(" 9. Update product");
        Console.WriteLine("10. Delete product");
        Console.WriteLine("11. Search products by name");
        Console.WriteLine("12. Filter products");
        Console.WriteLine("13. Register sale");
        Console.WriteLine("14. Restock product");
        Console.WriteLine("15. Report: Products with category");
        Console.WriteLine("16. Report: Low stock");
        Console.WriteLine("17. Report: Category summary");
        Console.WriteLine("18. Report: Sales per product");
        Console.WriteLine("19. Raise category prices by 10% (DataSet)");
        Console.WriteLine(" 0. Exit");
    }

    private void ListCategories()
    {
        var categories = _categoryRepo.GetAll();
        Console.WriteLine("-- - Categories-- - ");
        foreach (var c in categories)
        {
            Console.WriteLine($"{c.Id}: {c.Name}");
        }
    }

    private void FindCategoryById()
    {
        int id = ReadInt("Enter category id: ");
        var cat = _categoryRepo.GetById(id);
        if (cat == null)
        {
            Console.WriteLine("Category not found.");
        }
        else
        {
            Console.WriteLine($"{cat.Id}: {cat.Name}");
        }
    }

    private void AddCategory()
    {
        string name = ReadNonEmpty("Enter category name: ");
        int newId = _categoryRepo.Add(new Category { Name = name });
        if (newId > 0)
        {
            Console.WriteLine($"Category added with id {newId}.");
        }
    }

    private void UpdateCategory()
    {
        int id = ReadInt("Enter category id to update: ");
        string name = ReadNonEmpty("Enter new category name: ");
        bool updated = _categoryRepo.Update(new Category { Id = id, Name = name });
        Console.WriteLine(updated ? "Category updated." : "Category not updated.");
    }

    private void DeleteCategory()
    {
        int id = ReadInt("Enter category id to delete: ");
        bool deleted = _categoryRepo.Delete(id);
        if (deleted)
        {
            Console.WriteLine("Category deleted.");
        }
    }

    private void ListProducts()
    {
        var products = _productRepo.GetAll();
        Console.WriteLine("-- - Products-- - ");
        foreach (var p in products)
        {
            Console.WriteLine($"{p.Id}: {p.Name} | Price: {p.Price:F2} | Qty: {p.Quantity} | CategoryId: {p.CategoryId}");
        }
    }

    private void FindProductById()
    {
        int id = ReadInt("Enter product id: ");
        var p = _productRepo.GetById(id);
        if (p == null)
        {
            Console.WriteLine("Product not found.");
        }
        else
        {
            Console.WriteLine($"{p.Id}: {p.Name} | Price: {p.Price:F2} | Qty: {p.Quantity} | CategoryId: {p.CategoryId}");
        }
    }

    private void AddProduct()
    {
        string name = ReadOptionalString("Enter product name: ");
        decimal price = ReadDecimal("Enter price: ");
        int qty = ReadInt("Enter quantity: ");
        int categoryId = ReadInt("Enter category id: ");

        var errors = ValidateProduct(name, price, qty);
        if (errors.Count > 0)
        {
            foreach (var err in errors)
            {
                Console.WriteLine($"Validation error: {err}");
            }
            return;
        }

        if (_categoryRepo.GetById(categoryId) == null)
        {
            Console.WriteLine("Category not found.");
            return;
        }

        var product = new Product
        {
            Name = name,
            Price = price,
            Quantity = qty,
            CategoryId = categoryId
        };

        int newId = _productRepo.Add(product);
        Console.WriteLine($"Product added with id {newId}.");
    }

    private void UpdateProduct()
    {
        int id = ReadInt("Enter product id to update: ");
        var existing = _productRepo.GetById(id);
        if (existing == null)
        {
            Console.WriteLine("Product not found.");
            return;
        }

        string name = ReadOptionalString("Enter new product name: ");
        decimal price = ReadDecimal("Enter new price: ");
        int qty = ReadInt("Enter new quantity: ");
        int categoryId = ReadInt("Enter new category id: ");

        var errors = ValidateProduct(name, price, qty);
        if (errors.Count > 0)
        {
            foreach (var err in errors)
            {
                Console.WriteLine($"Validation error: {err}");
            }
            return;
        }

        if (_categoryRepo.GetById(categoryId) == null)
        {
            Console.WriteLine("Category not found.");
            return;
        }

        bool updated = _productRepo.Update(new Product
        {
            Id = id,
            Name = name,
            Price = price,
            Quantity = qty,
            CategoryId = categoryId
        });

        Console.WriteLine(updated ? "Product updated." : "Product not updated.");
    }

    private void DeleteProduct()
    {
        int id = ReadInt("Enter product id to delete: ");
        bool deleted = _productRepo.Delete(id);
        Console.WriteLine(deleted ? "Product deleted." : "Product not found or not deleted.");
    }

    private void SearchProducts()
    {
        Console.Write("Enter search term: ");
        string term = Console.ReadLine() ?? "";

        var results = _productRepo.SearchByName(term);
        Console.WriteLine($"Found {results.Count} product(s):");
        foreach (var p in results)
        {
            Console.WriteLine($"{p.Id}: {p.Name} | Price: {p.Price:F2} | Qty: {p.Quantity}");
        }
    }

    private void FilterProducts()
    {
        int? categoryId = ReadNullableInt("Category id (or empty to skip): ");
        decimal? minPrice = ReadNullableDecimal("Min price (or empty to skip): ");
        decimal? maxPrice = ReadNullableDecimal("Max price (or empty to skip): ");

        var results = _productRepo.Filter(categoryId, minPrice, maxPrice);
        Console.WriteLine($"Filtered {results.Count} product(s):");
        foreach (var p in results)
        {
            Console.WriteLine($"{p.Id}: {p.Name} | Price: {p.Price:F2} | Qty: {p.Quantity} | CategoryId: {p.CategoryId}");
        }
    }

    private void RegisterSale()
    {
        int productId = ReadInt("Enter product id: ");
        int qty = ReadInt("Enter quantity to sell: ");
        string message = _stockService.RegisterSale(productId, qty);
        Console.WriteLine(message);
    }

    private void RestockProduct()
    {
        int productId = ReadInt("Enter product id: ");
        int qty = ReadInt("Enter quantity to add: ");
        string message = _stockService.Restock(productId, qty);
        Console.WriteLine(message);
    }

    private void PrintLowStockReport()
    {
        int threshold = ReadInt("Enter stock threshold: ");
        _productRepo.PrintLowStock(threshold);
    }

    private void RaiseCategoryPricesWithDataSet()
    {
        ListCategories();
        int categoryId = ReadInt("Enter category id to raise prices by 10%: ");

        using var conn = _db.GetConnection();
        conn.Open();

        const string selectSql = "SELECT id, name, price, quantity, category_id FROM products ORDER BY id;";
        using var adapter = new NpgsqlDataAdapter(selectSql, conn);
        using var builder = new NpgsqlCommandBuilder(adapter);

        var ds = new DataSet();
        adapter.Fill(ds, "products");

        DataTable table = ds.Tables["products"]!;
        foreach (DataRow row in table.Rows)
        {
            int rowCatId = Convert.ToInt32(row["category_id"]);
            if (rowCatId == categoryId)
            {
                decimal currentPrice = Convert.ToDecimal(row["price"]);
                decimal newPrice = Math.Round(currentPrice * 1.10m, 2);
                row["price"] = newPrice;
            }
        }

        DataTable? changes = table.GetChanges(DataRowState.Modified);
        int modifiedCount = changes?.Rows.Count ?? 0;
        Console.WriteLine($"Rows pending as Modified: {modifiedCount}");

        adapter.Update(ds, "products");

        var verifyDs = new DataSet();
        using var verifyAdapter = new NpgsqlDataAdapter(selectSql, conn);
        verifyAdapter.Fill(verifyDs, "products");

        Console.WriteLine("-- - Updated prices(new DataSet)-- - ");
        foreach (DataRow row in verifyDs.Tables["products"]!.Rows)
        {
            Console.WriteLine($"{row["id"]}: {row["name"]} | Price: {Convert.ToDecimal(row["price"]):F2}");
        }
    }

    private List<string> ValidateProduct(string name, decimal price, int quantity)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Name is required.");
        }

        if (price <= 0)
        {
            errors.Add("Price must be greater than 0.");
        }

        if (quantity < 0)
        {
            errors.Add("Quantity must be 0 or greater.");
        }

        return errors;
    }

    private string ReadOptionalString(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine() ?? "";
    }

    private string ReadNonEmpty(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(input))
            {
                return input;
            }
            Console.WriteLine("Input cannot be empty. Try again.");
        }
    }

    private int ReadInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine()?.Trim() ?? "";
            if (int.TryParse(input, out int value))
            {
                return value;
            }
            Console.WriteLine("Please enter a valid integer.");
        }
    }

    private decimal ReadDecimal(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = (Console.ReadLine() ?? "").Trim().Replace(',', '.');
            if (decimal.TryParse(input, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal value))
            {
                return value;
            }
            Console.WriteLine("Please enter a valid number.");
        }
    }

    private int? ReadNullableInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            if (int.TryParse(input, out int value))
            {
                return value;
            }
            Console.WriteLine("Please enter a valid integer or leave empty.");
        }
    }

    private decimal? ReadNullableDecimal(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = (Console.ReadLine() ?? "").Trim().Replace(',', '.');
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            if (decimal.TryParse(input, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal value))
            {
                return value;
            }
            Console.WriteLine("Please enter a valid number or leave empty.");
        }
    }
}
