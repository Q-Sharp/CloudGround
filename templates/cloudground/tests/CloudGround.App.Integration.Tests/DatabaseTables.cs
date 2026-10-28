using Npgsql;

namespace CloudGround.App.Integration.Tests;

internal static class DatabaseTables
{
    public static async Task<List<string>> InSchemaAsync(DistributedApplication app, string schema)
    {
        var cancellation = TestContext.Current.CancellationToken;
        var connectionString = await app.GetConnectionStringAsync("database", cancellation);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellation);
        await using var command = new NpgsqlCommand("select table_name from information_schema.tables where table_schema = @schema", connection);
        command.Parameters.AddWithValue("schema", schema);

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        while (await reader.ReadAsync(cancellation))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}
