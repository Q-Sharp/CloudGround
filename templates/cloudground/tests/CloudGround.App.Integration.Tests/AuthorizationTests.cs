using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CloudGround.App.Integration.Tests;

public sealed partial class AuthorizationTests(AppHostFixture fixture)
{
    private static readonly string[] WriteMethods = ["post", "put", "patch", "delete"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Every_write_endpoint_rejects_anonymous_callers()
    {
        using var client = fixture.CreateApiClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative), Cancellation));

        foreach (var path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject().Where(operation => WriteMethods.Contains(operation.Name)))
            {
                var uri = new Uri(RouteValue().Replace(path.Name, Guid.CreateVersion7().ToString()), UriKind.Relative);
                using var request = new HttpRequestMessage(new HttpMethod(method.Name.ToUpperInvariant()), uri) { Content = JsonContent.Create(new { }) };

                using var response = await client.SendAsync(request, Cancellation);

                Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"{method.Name.ToUpperInvariant()} {path.Name} answered {response.StatusCode}");
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            }
        }
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex RouteValue();
}
