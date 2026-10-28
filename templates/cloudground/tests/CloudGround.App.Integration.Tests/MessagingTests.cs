namespace CloudGround.App.Integration.Tests;

public sealed class MessagingTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Wolverine_storage_lives_in_the_wolverine_schema()
    {
        var tables = await DatabaseTables.InSchemaAsync(fixture.App, "wolverine");

        Assert.Contains("wolverine_incoming_envelopes", tables);
        Assert.Contains("wolverine_outgoing_envelopes", tables);
    }
}
