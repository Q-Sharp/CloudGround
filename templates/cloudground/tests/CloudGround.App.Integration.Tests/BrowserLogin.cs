using System.Net;
using System.Text.RegularExpressions;

namespace CloudGround.App.Integration.Tests;

internal sealed class BrowserSession(HttpClient client, LocalhostBrowserHandler browser, Uri landingPage) : IDisposable
{
    public HttpClient Client { get; } = client;

    public Uri LandingPage { get; } = landingPage;

    public string? XsrfToken => browser.Cookie("XSRF-TOKEN");

    public void Dispose() => Client.Dispose();
}

internal static partial class BrowserLogin
{
    public static async Task<BrowserSession> LogInAsync(AppHostFixture fixture, string user, string? returnUrl = null)
    {
        var cancellation = TestContext.Current.CancellationToken;
        var browser = new LocalhostBrowserHandler();
        var client = new HttpClient(browser)
        {
            BaseAddress = fixture.App.GetEndpoint("api", "http"),
        };

        var query = returnUrl is null ? string.Empty : $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
        using var loginPage = await client.GetAsync(new Uri($"/account/login{query}", UriKind.Relative), cancellation);
        var form = LoginFormTag().Match(await loginPage.Content.ReadAsStringAsync(cancellation));
        Assert.True(form.Success, $"No Keycloak login form at {loginPage.RequestMessage?.RequestUri}");
        var action = FormAction().Match(form.Value).Groups["action"].Value;

        using var credentials = new FormUrlEncodedContent(new Dictionary<string, string> { ["username"] = user, ["password"] = user });
        using var landing = await client.PostAsync(new Uri(WebUtility.HtmlDecode(action)), credentials, cancellation);

        return new BrowserSession(client, browser, landing.RequestMessage!.RequestUri!);
    }

    [GeneratedRegex("""<form\b[^>]*\bid="kc-form-login"[^>]*>""")]
    private static partial Regex LoginFormTag();

    [GeneratedRegex(@"\baction=""(?<action>[^""]+)""")]
    private static partial Regex FormAction();
}
