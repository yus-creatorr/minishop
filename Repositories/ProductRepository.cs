using MiniShop.Data;
using MiniShop.Models;
using Npgsql;

namespace MiniShop.Repositories;

public class ProductRepository
{
    private readonly Database _db;

    public ProductRepository(Database db)
    {
        _db = db;
    }

    public List<Product> GetAll()
    {
        var list = new List<Product>();

        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = @"
            SELECT id, name, price, quantity, category_id
            FROM products
            ORDER BY id;
        ";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(MapProduct(reader));
        }

        return list;
    }

    public Product? GetById(int id)
    {
        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = @"
            SELECT id, name, price, quantity, category_id
            FROM products
            WHERE id = @id;
        ";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapProduct(reader);
        }

        return null;
    }

    public int Add(Product p)
    {
        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = @"
            INSERT INTO products (name, price, quantity, category_id)
            VALUES (@name, @price, @quantity, @category_id)
            RETURNING id;
        ";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name", p.Name.Trim());
        cmd.Parameters.AddWithValue("price", p.Price);
        cmd.Parameters.AddWithValue("quantity", p.Quantity);
        cmd.Parameters.AddWithValue("category_id", p.CategoryId);

        object? result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public bool Update(Product p)
    {
        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = @"
            UPDATE products
            SET name = @name,
                price = @price,
                quantity = @quantity,
                category_id = @category_id
            WHERE id = @id;
        ";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", p.Id);
        cmd.Parameters.AddWithValue("name", p.Name.Trim());
        cmd.Parameters.AddWithValue("price", p.Price);
        cmd.Parameters.AddWithValue("quantity", p.Quantity);
        cmd.Parameters.AddWithValue("category_id", p.CategoryId);

        int rows = cmd.ExecuteNonQuery();
        return rows > 0;
    }

    public bool Delete(int id)
    {
        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = "DELETE FROM products WHERE id = @id;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        int rows = cmd.ExecuteNonQuery();
        return rows > 0;
    }

    public List<Product> SearchByName(string term)
    {
        var list = new List<Product>();

        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = @"
            SELECT id, name, price, quantity, category_id
            FROM products
            WHERE name ILIKE @term
            ORDER BY id;
        ";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("term", $"%{term}%");

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapProduct(reader));
        }

        return list;
    }

    public List<Product> Filter(int? categoryId, decimal? minPrice, decimal? maxPrice)
    {
        var list = new List<Product>();
        var conditions = new List<string>();

        using var conn = _db.GetConnection();
        conn.Open();

        using var cmd = new NpgsqlCommand();
        cmd.Connection = conn;

        if (categoryId.HasValue)
        {
            conditions.Add("category_id = @category_id");
            cmd.Parameters.AddWithValue("category_id", categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            conditions.Add("price >= @min_price");
            cmd.Parameters.AddWithValue("min_price", minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            conditions.Add("price <= @max_price");
            cmd.Parameters.AddWithValue("max_price", maxPrice.Value);
        }

        string sql = "SELECT id, name, price, quantity, category_id FROM products";
        if (conditions.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", conditions);
        }
        sql += " ORDER BY id;";

        cmd.CommandText = sql;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapProduct(reader));
        }

        return list;
    }

    public void PrintProductsWithCategory()
    {
        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = @"
            SELECT p.name, p.price, p.quantity, c.name AS category_name
            FROM products p
            INNER JOIN categories c ON p.category_id = c.id
            ORDER BY p.id;
        ";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        Console.WriteLine("-- - Products with category-- - ");
        while (reader.Read())
        {
            string name = reader.GetString(0);
            decimal price = reader.GetDecimal(1);
            int qty = reader.GetInt32(2);
            string categoryName = reader.GetString(3);
            Console.WriteLine($"{name} | Price: {price:F2} | Qty: {qty} | Category: {categoryName}");
        }
    }

    public void PrintLowStock(int threshold)
    {
        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = @"
            SELECT p.name, p.price, p.quantity, c.name AS category_name
            FROM products p
            INNER JOIN categories c ON p.category_id = c.id
            WHERE p.quantity < @threshold
            ORDER BY p.quantity ASC, p.id ASC;
        ";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("threshold", threshold);

        using var reader = cmd.ExecuteReader();

        Console.WriteLine($"-- - Low stock(quantity < { threshold}) ---");
        while (reader.Read())
        {
            string name = reader.GetString(0);
            decimal price = reader.GetDecimal(1);
            int qty = reader.GetInt32(2);
            string categoryName = reader.GetString(3);
            Console.WriteLine($"{name} | Price: {price:F2} | Qty: {qty} | Category: {categoryName}");
        }
    }

    public void PrintCategorySummary()
    {
        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = @"
            SELECT
                c.name,
                COUNT(p.id) AS product_count,
                COALESCE(SUM(p.quantity * p.price), 0) AS stock_value
            FROM categories c
            LEFT JOIN products p ON p.category_id = c.id
            GROUP BY c.id, c.name
            ORDER BY c.name;
        ";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        Console.WriteLine("Category summary: ");
        while (reader.Read())
        {
            string catName = reader.GetString(0);
            long count = reader.GetInt64(1);
            decimal stockValue = reader.GetDecimal(2);
            Console.WriteLine($"{catName} — {count} product(s) — stock value: {stockValue:F2}");
        }
    }

    public void PrintSalesPerProduct()
    {
        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = @"
            SELECT
                p.name,
                COALESCE(SUM(CASE WHEN sm.change_qty < 0 THEN -sm.change_qty ELSE 0 END), 0) AS total_sold
            FROM products p
            LEFT JOIN stock_movements sm ON sm.product_id = p.id
            GROUP BY p.id, p.name
            ORDER BY total_sold DESC, p.id ASC;
        ";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        Console.WriteLine("-- - Sales per product-- - ");
        while (reader.Read())
        {
            string name = reader.GetString(0);
            long totalSold = reader.GetInt64(1);
            Console.WriteLine($"{name} — sold: {totalSold}");
        }
    }

    private Product MapProduct(NpgsqlDataReader reader)
    {
        return new Product
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Price = reader.GetDecimal(2),
            Quantity = reader.GetInt32(3),
            CategoryId = reader.GetInt32(4)
        };
    }
}
