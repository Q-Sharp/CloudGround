using System.Net;
using System.Text.Json;
using Npgsql;

namespace CloudGround.App.Integration.Tests;

internal static class ModuleAssertions
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public static void AssertProblem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    public static async Task AssertValidationProblemAsync(HttpResponseMessage response, string field)
    {
        AssertProblem(response, HttpStatusCode.BadRequest);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
        var errors = problem.RootElement.GetProperty("errors").EnumerateObject().Select(error => error.Name);
        Assert.Contains(errors, error => error.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<List<string>> TablesInSchemaAsync(DistributedApplication app, string schema)
    {
        var connectionString = await app.GetConnectionStringAsync("database", Cancellation);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Cancellation);
        await using var command = new NpgsqlCommand("select table_name from information_schema.tables where table_schema = @schema", connection);
        command.Parameters.AddWithValue("schema", schema);

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(Cancellation);
        while (await reader.ReadAsync(Cancellation))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}
