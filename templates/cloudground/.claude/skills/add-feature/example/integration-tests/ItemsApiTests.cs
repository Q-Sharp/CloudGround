namespace CloudGround.App.Integration.Tests;

public sealed class ItemsApiTests(AppHostFixture fixture)
{
    private static readonly Uri ItemsUri = new("/api/modulename", UriKind.Relative);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Created_item_can_be_read_anonymously()
    {
        using var editor = await fixture.CreateApiClientAsync("alice");
        using var created = await editor.PostAsJsonAsync(ItemsUri, new { Name = "First item" }, Cancellation);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var anonymous = fixture.CreateApiClient();
        var item = await anonymous.GetFromJsonAsync<ItemDto>(created.Headers.Location, Cancellation);

        Assert.NotNull(item);
        Assert.Equal("First item", item.Name);
    }

    [Fact]
    public async Task Writes_need_the_editor_role()
    {
        using var client = await fixture.CreateApiClientAsync("bob");

        using var response = await client.PostAsJsonAsync(ItemsUri, new { Name = "Written by bob" }, Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Blank_name_is_rejected()
    {
        using var client = await fixture.CreateApiClientAsync("alice");

        using var response = await client.PostAsJsonAsync(ItemsUri, new { Name = "   " }, Cancellation);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record ItemDto(Guid Id, string Name, DateTimeOffset CreatedAt);
}
