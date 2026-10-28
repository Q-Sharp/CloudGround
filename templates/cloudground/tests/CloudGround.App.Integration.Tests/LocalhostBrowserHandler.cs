using System.Globalization;
using System.Net;

namespace CloudGround.App.Integration.Tests;

internal sealed class LocalhostBrowserHandler() : DelegatingHandler(new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false })
{
    private const int MaximumRedirects = 20;

    private readonly Dictionary<string, string> cookies = [];

    public string? Cookie(string name) => cookies.GetValueOrDefault(name);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var redirects = 0; ; redirects++)
        {
            if (cookies.Count > 0)
            {
                request.Headers.Remove("Cookie");
                request.Headers.Add("Cookie", string.Join("; ", cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
            }

            var response = await base.SendAsync(request, cancellationToken);
            Remember(response);

            if (response.Headers.Location is not { } location || !IsRedirect(response.StatusCode) || redirects == MaximumRedirects)
            {
                response.RequestMessage = request;
                return response;
            }

            var target = location.IsAbsoluteUri ? location : new Uri(request.RequestUri!, location);
            var keepsMethod = response.StatusCode is HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
            var next = new HttpRequestMessage(keepsMethod ? request.Method : HttpMethod.Get, target) { Content = keepsMethod ? request.Content : null };
            foreach (var header in request.Headers.Where(header => header.Key is not "Cookie"))
            {
                next.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            response.Dispose();
            request = next;
        }
    }

    private static bool IsRedirect(HttpStatusCode status) =>
        status is HttpStatusCode.Moved or HttpStatusCode.Found or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    private void Remember(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            return;
        }

        foreach (var setCookie in setCookies)
        {
            var parts = setCookie.Split(';', StringSplitOptions.TrimEntries);
            var separator = parts[0].IndexOf('=', StringComparison.Ordinal);
            var name = parts[0][..separator];
            var value = parts[0][(separator + 1)..];

            if (value.Length == 0 || parts.Any(IsExpiry))
            {
                cookies.Remove(name);
            }
            else
            {
                cookies[name] = value;
            }
        }
    }

    private static bool IsExpiry(string attribute) =>
        attribute.Equals("max-age=0", StringComparison.OrdinalIgnoreCase)
        || (attribute.StartsWith("expires=", StringComparison.OrdinalIgnoreCase)
            && DateTimeOffset.TryParse(attribute["expires=".Length..], CultureInfo.InvariantCulture, out var expires)
            && expires < DateTimeOffset.UtcNow);
}
