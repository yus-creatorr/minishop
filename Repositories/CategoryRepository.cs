using MiniShop.Data;
using MiniShop.Models;
using Npgsql;

namespace MiniShop.Repositories;

public class CategoryRepository
{
    private readonly Database _db;

    public CategoryRepository(Database db)
    {
        _db = db;
    }

    public List<Category> GetAll()
    {
        var list = new List<Category>();

        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = "SELECT id, name FROM categories ORDER BY id;";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return list;
    }

    public Category? GetById(int id)
    {
        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = "SELECT id, name FROM categories WHERE id = @id;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            };
        }

        return null;
    }

    public int Add(Category c)
    {
        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = "INSERT INTO categories (name) VALUES (@name) RETURNING id;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name", c.Name.Trim());

        try
        {
            object? result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            Console.WriteLine("Category already exists.");
            return 0;
        }
    }

    public bool Update(Category c)
    {
        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = "UPDATE categories SET name = @name WHERE id = @id;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", c.Id);
        cmd.Parameters.AddWithValue("name", c.Name.Trim());

        try
        {
            int rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            Console.WriteLine("Category already exists.");
            return false;
        }
    }

    public bool Delete(int id)
    {
        using var conn = _db.GetConnection();
        conn.Open();

        const string sql = "DELETE FROM categories WHERE id = @id;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        try
        {
            int rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }
        catch (PostgresException ex) when (ex.SqlState == "23503")
        {
            Console.WriteLine("Cannot delete: category has products.");
            return false;
        }
    }
}
