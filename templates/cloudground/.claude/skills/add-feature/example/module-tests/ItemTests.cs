using CloudGround.App.Modules.ModuleName.Domain;

namespace CloudGround.App.Modules.ModuleName.Tests;

public sealed class ItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Created_item_keeps_its_name_and_creation_time()
    {
        var item = Item.Create("First item", Now);

        Assert.Equal("First item", item.Name);
        Assert.Equal(Now, item.CreatedAt);
    }
}
