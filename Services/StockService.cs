using MiniShop.Data;
using Npgsql;

namespace MiniShop.Services;

public class StockService
{
    private readonly Database _db;

    public StockService(Database db)
    {
        _db = db;
    }

    public string RegisterSale(int productId, int qty)
    {
        if (qty <= 0)
        {
            return "Quantity must be greater than 0.";
        }

        using var conn = _db.GetConnection();
        conn.Open();

        using var tx = conn.BeginTransaction();

        try
        {
            const string selectSql = "SELECT quantity FROM products WHERE id = @id FOR UPDATE;";
            int currentQty;

            using (var selectCmd = new NpgsqlCommand(selectSql, conn, tx))
            {
                selectCmd.Parameters.AddWithValue("id", productId);
                object? result = selectCmd.ExecuteScalar();

                if (result == null)
                {
                    tx.Rollback();
                    return "Product not found.";
                }

                currentQty = Convert.ToInt32(result);
            }

            if (qty > currentQty)
            {
                tx.Rollback();
                return $"Cannot sell: only {currentQty} in stock.";
            }

            int newQty = currentQty - qty;

            const string updateSql = "UPDATE products SET quantity = @qty WHERE id = @id;";
            using (var updateCmd = new NpgsqlCommand(updateSql, conn, tx))
            {
                updateCmd.Parameters.AddWithValue("qty", newQty);
                updateCmd.Parameters.AddWithValue("id", productId);
                updateCmd.ExecuteNonQuery();
            }

            const string insertMovementSql = @"
                INSERT INTO stock_movements (product_id, change_qty)
                VALUES (@product_id, @change_qty);
            ";
            using (var movementCmd = new NpgsqlCommand(insertMovementSql, conn, tx))
            {
                movementCmd.Parameters.AddWithValue("product_id", productId);
                movementCmd.Parameters.AddWithValue("change_qty", -qty);
                movementCmd.ExecuteNonQuery();
            }

            tx.Commit();
            return $"Sale registered. Remaining: {newQty}";
        }
        catch (Exception ex)
        {
            try
            {
                tx.Rollback();
            }
            catch
            {
            }

            return $"Transaction failed: {ex.Message}";
        }
    }

    public string Restock(int productId, int qty)
    {
        if (qty <= 0)
        {
            return "Quantity must be greater than 0.";
        }

        using var conn = _db.GetConnection();
        conn.Open();

        using var tx = conn.BeginTransaction();

        try
        {
            const string selectSql = "SELECT quantity FROM products WHERE id = @id FOR UPDATE;";
            int currentQty;

            using (var selectCmd = new NpgsqlCommand(selectSql, conn, tx))
            {
                selectCmd.Parameters.AddWithValue("id", productId);
                object? result = selectCmd.ExecuteScalar();

                if (result == null)
                {
                    tx.Rollback();
                    return "Product not found.";
                }

                currentQty = Convert.ToInt32(result);
            }

            int newQty = currentQty + qty;

            const string updateSql = "UPDATE products SET quantity = @qty WHERE id = @id;";
            using (var updateCmd = new NpgsqlCommand(updateSql, conn, tx))
            {
                updateCmd.Parameters.AddWithValue("qty", newQty);
                updateCmd.Parameters.AddWithValue("id", productId);
                updateCmd.ExecuteNonQuery();
            }

            const string insertMovementSql = @"
                INSERT INTO stock_movements (product_id, change_qty)
                VALUES (@product_id, @change_qty);
            ";
            using (var movementCmd = new NpgsqlCommand(insertMovementSql, conn, tx))
            {
                movementCmd.Parameters.AddWithValue("product_id", productId);
                movementCmd.Parameters.AddWithValue("change_qty", qty);
                movementCmd.ExecuteNonQuery();
            }

            tx.Commit();
            return $"Restocked. New quantity: {newQty}";
        }
        catch (Exception ex)
        {
            try
            {
                tx.Rollback();
            }
            catch
            {
            }

            return $"Transaction failed: {ex.Message}";
        }
    }
}
