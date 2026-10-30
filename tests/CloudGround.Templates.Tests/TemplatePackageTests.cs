namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class TemplatePackageTests(TemplatePackage package)
{
    [Fact]
    public async Task Solutions_of_an_earlier_test_are_removed()
    {
        var solution = await package.GenerateAsync("Earlier.App", "--sample", "false");

        package.RemoveSolutionsExceptOf("a later test");

        Assert.False(Directory.Exists(solution.Root));
    }

    [Fact]
    public async Task Solutions_of_the_running_test_stay()
    {
        var first = await package.GenerateAsync("First.Shop", "--sample", "false");

        await package.GenerateAsync("Second.Shop", "--sample", "false");

        Assert.True(Directory.Exists(first.Root));
    }
}
