namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class ContainerImageTests(TemplatePackage package)
{
    private const string ServiceBaseImage = "mcr.microsoft.com/dotnet/aspnet:10.0-resolute-chiseled-extra";
    private const string PregeneratedHandlers = "Using pre-generated Wolverine HandlerRegistry";
    private static readonly TimeSpan StartupLimit = TimeSpan.FromSeconds(60);

    private static readonly Dictionary<string, string> ProductionWithoutInfrastructure = new()
    {
        ["ASPNETCORE_ENVIRONMENT"] = "Production",
        ["ASPNETCORE_URLS"] = "http://127.0.0.1:0",
        ["ConnectionStrings__database"] = "Host=127.0.0.1;Port=1;Username=app;Password=app;Database=app",
        ["ConnectionStrings__messaging"] = "amqp://guest:guest@127.0.0.1:1",
        ["ConnectionStrings__cache"] = "127.0.0.1:1",
    };

    [Theory]
    [InlineData("Api")]
    [InlineData("Bff")]
    public async Task Services_build_on_ubuntu_lts_chiseled_images(string service)
    {
        var solution = await package.GenerateAsync("Images.App");
        var project = Path.Combine("src", $"Images.App.{service}", $"Images.App.{service}.csproj");

        var result = await DotnetCli.RunAsync(solution.Root, "msbuild", project, "-restore", "-target:ComputeContainerBaseImage", "-getProperty:ContainerBaseImage");

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Equal(ServiceBaseImage, LastLine(result.Output));
    }

    [Fact]
    public async Task Published_api_loads_pregenerated_handlers()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");
        var output = Path.Combine(solution.Root, "artifacts", "api");
        var publish = await DotnetCli.RunAsync(solution.Root, "publish", Path.Combine("src", "Contoso.Shop.Api"), "-c", "Release", "-o", output);
        Assert.True(publish.ExitCode == 0, publish.Output);

        var run = await ProcessRunner.RunUntilAsync(
            "dotnet",
            output,
            ProductionWithoutInfrastructure,
            line => line.Contains(PregeneratedHandlers, StringComparison.Ordinal),
            StartupLimit,
            "Contoso.Shop.Api.dll");

        Assert.Contains(PregeneratedHandlers, run.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("MissingPreBuiltTypesException", run.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Publishing_the_api_without_a_build_fails_with_the_reason()
    {
        var solution = await package.GenerateAsync("NoBuild.App");
        var api = Path.Combine("src", "NoBuild.App.Api");
        var build = await DotnetCli.RunAsync(solution.Root, "build", api, "-c", "Release");
        Assert.True(build.ExitCode == 0, build.Output);

        var publish = await DotnetCli.RunAsync(solution.Root, "publish", api, "-c", "Release", "--no-build", "-o", Path.Combine(solution.Root, "artifacts", "api"));

        Assert.NotEqual(0, publish.ExitCode);
        Assert.Contains("NoBuild.App.Api cannot be published with --no-build", publish.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stale_generated_handlers_do_not_break_the_build()
    {
        var solution = await package.GenerateAsync("Stale.App");
        var api = Path.Combine(solution.Root, "src", "Stale.App.Api");
        var generated = Directory.CreateDirectory(Path.Combine(api, "Internal", "Generated", "WolverineHandlers")).FullName;
        await File.WriteAllTextAsync(Path.Combine(generated, "RemovedHandler.cs"), "this is not C#", TestContext.Current.CancellationToken);

        var build = await DotnetCli.RunAsync(api, "build");

        Assert.True(build.ExitCode == 0, build.Output);
    }

    private static string LastLine(string output) =>
        output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[^1];
}
