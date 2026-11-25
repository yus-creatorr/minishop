using Npgsql;

namespace MiniShop.Data;

public class Database
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=minishop;Username=postgres;Password=P7U0O8I9";

    public NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }

    public bool TestConnection()
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database connection failed: {ex.Message}");
            return false;
        }
    }

    public void Initialize()
    {
        using var conn = GetConnection();
        conn.Open();

        string schemaSql = @"
            CREATE TABLE IF NOT EXISTS categories (
                id    SERIAL PRIMARY KEY,
                name  VARCHAR(100) NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS products (
                id           SERIAL PRIMARY KEY,
                name         VARCHAR(150) NOT NULL,
                price        NUMERIC(10,2) NOT NULL CHECK (price > 0),
                quantity     INT NOT NULL DEFAULT 0 CHECK (quantity >= 0),
                category_id  INT NOT NULL REFERENCES categories(id)
            );

            CREATE TABLE IF NOT EXISTS stock_movements (
                id          SERIAL PRIMARY KEY,
                product_id  INT NOT NULL REFERENCES products(id),
                change_qty  INT NOT NULL CHECK (change_qty <> 0),
                moved_at    TIMESTAMP NOT NULL DEFAULT NOW()
            );
        ";

        using (var cmd = new NpgsqlCommand(schemaSql, conn))
        {
            cmd.ExecuteNonQuery();
        }

        SeedData(conn);
    }

    private void SeedData(NpgsqlConnection conn)
    {
        string[] categoryNames = { "Accessories", "Cables", "Spare parts" };

        foreach (var catName in categoryNames)
        {
            const string insertCategorySql = @"
                INSERT INTO categories (name)
                VALUES (@name)
                ON CONFLICT (name) DO NOTHING;
            ";

            using var cmd = new NpgsqlCommand(insertCategorySql, conn);
            cmd.Parameters.AddWithValue("name", catName);
            cmd.ExecuteNonQuery();
        }

        int accessoriesId = GetCategoryId(conn, "Accessories");
        int cablesId = GetCategoryId(conn, "Cables");

        var seedProducts = new List<(string Name, decimal Price, int Quantity, int CategoryId)>
        {
            ("Wireless Mouse", 25.00m, 10, accessoriesId),
            ("Mechanical Keyboard", 75.00m, 5, accessoriesId),
            ("Mouse Pad", 15.00m, 20, accessoriesId),
            ("USB-C Cable", 12.00m, 30, cablesId),
            ("HDMI Cable", 18.00m, 15, cablesId),
            ("DisplayPort Cable", 20.00m, 8, cablesId)
        };

        foreach (var item in seedProducts)
        {
            const string existsSql = "SELECT COUNT(*) FROM products WHERE name = @name;";
            using var checkCmd = new NpgsqlCommand(existsSql, conn);
            checkCmd.Parameters.AddWithValue("name", item.Name);
            long count = (long)checkCmd.ExecuteScalar()!;

            if (count == 0)
            {
                const string insertProductSql = @"
                    INSERT INTO products (name, price, quantity, category_id)
                    VALUES (@name, @price, @quantity, @category_id);
                ";

                using var insertCmd = new NpgsqlCommand(insertProductSql, conn);
                insertCmd.Parameters.AddWithValue("name", item.Name);
                insertCmd.Parameters.AddWithValue("price", item.Price);
                insertCmd.Parameters.AddWithValue("quantity", item.Quantity);
                insertCmd.Parameters.AddWithValue("category_id", item.CategoryId);
                insertCmd.ExecuteNonQuery();
            }
        }
    }

    private int GetCategoryId(NpgsqlConnection conn, string name)
    {
        const string sql = "SELECT id FROM categories WHERE name = @name;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name", name);
        object? result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : 0;
    }
}
