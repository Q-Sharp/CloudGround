namespace CloudGround.App.Integration.Tests;

internal sealed class KeycloakAdmin(DistributedApplication app) : IAsyncDisposable
{
    private const string ApiClientId = "cloudground-app-api";
    private const string AccessTokenLifespan = "access.token.lifespan";
    private const string RealmAccessTokenSeconds = "300";

    private readonly HttpClient client = new() { BaseAddress = app.GetEndpoint("keycloak", "http") };

    private static string Realm => $"/admin/realms/{Tokens.Realm}";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public async Task<string> CreateEditorAsync(string name)
    {
        await AuthorizeAsync();
        using var created = await client.PostAsJsonAsync($"{Realm}/users", new
        {
            username = name,
            enabled = true,
            emailVerified = true,
            firstName = name,
            lastName = "Example",
            email = $"{name}@example.com",
            credentials = new[] { new { type = "password", value = name, temporary = false } },
        }, Cancellation);
        created.EnsureSuccessStatusCode();
        var id = created.Headers.Location!.Segments[^1];
        using var granted = await client.PostAsJsonAsync($"{Realm}/users/{id}/role-mappings/realm", new[] { await EditorRoleAsync() }, Cancellation);
        granted.EnsureSuccessStatusCode();
        return id;
    }

    public async Task TakeEditorRoleAsync(string userId)
    {
        await AuthorizeAsync();
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{Realm}/users/{userId}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { await EditorRoleAsync() }),
        };
        using var response = await client.SendAsync(request, Cancellation);
        response.EnsureSuccessStatusCode();
    }

    public async Task BlockAsync(string userId)
    {
        await AuthorizeAsync();
        using var response = await client.PutAsJsonAsync($"{Realm}/users/{userId}", new { enabled = false }, Cancellation);
        response.EnsureSuccessStatusCode();
    }

    public Task RefreshAccessTokensOnEveryRequestAsync() => SetAccessTokenLifespanAsync("60");

    public async ValueTask DisposeAsync()
    {
        await SetAccessTokenLifespanAsync(RealmAccessTokenSeconds);
        client.Dispose();
    }

    private async Task<JsonElement> EditorRoleAsync() =>
        await client.GetFromJsonAsync<JsonElement>($"{Realm}/roles/editor", Cancellation);

    private async Task AuthorizeAsync()
    {
        var keycloak = app.Services.GetRequiredService<DistributedApplicationModel>().Resources.OfType<KeycloakResource>().Single();
        using var credentials = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = keycloak.AdminUserNameParameter is { } userName ? (await userName.GetValueAsync(Cancellation))! : "admin",
            ["password"] = (await keycloak.AdminPasswordParameter.GetValueAsync(Cancellation))!,
        });
        client.DefaultRequestHeaders.Authorization = null;
        using var response = await client.PostAsync(new Uri("/realms/master/protocol/openid-connect/token", UriKind.Relative), credentials, Cancellation);
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.GetProperty("access_token").GetString());
    }

    private async Task SetAccessTokenLifespanAsync(string seconds)
    {
        await AuthorizeAsync();
        var clients = await client.GetFromJsonAsync<JsonArray>($"{Realm}/clients?clientId={ApiClientId}", Cancellation);
        var apiClient = clients![0]!.AsObject();
        apiClient["attributes"]![AccessTokenLifespan] = seconds;
        using var response = await client.PutAsJsonAsync($"{Realm}/clients/{apiClient["id"]}", apiClient, Cancellation);
        response.EnsureSuccessStatusCode();
    }
}
