using System.Net;
using System.Text.Json;

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
}
