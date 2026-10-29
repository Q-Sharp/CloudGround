namespace CloudGround.App.Integration.Tests;

[CollectionDefinition(Name)]
public sealed class PublishedDeployment : ICollectionFixture<PublishedCompose>
{
    public const string Name = "PublishedDeployment";
}

public sealed class PublishedCompose : IAsyncLifetime
{
    private const string ServiceIndent = "  ";
    private const string ServicesKey = "services:";

    private Dictionary<string, string> services = [];

    public string OutputDirectory { get; } = Directory.CreateTempSubdirectory("compose-").FullName;

    public IReadOnlyCollection<string> ServiceNames => services.Keys;

    public async ValueTask InitializeAsync()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(
            ["--operation", "publish", "--step", "publish", "--output-path", OutputDirectory],
            cancellation);
        await using var app = await appHost.BuildAsync(cancellation);
        await app.RunAsync(cancellation);

        services = ReadServices(await File.ReadAllLinesAsync(Path.Combine(OutputDirectory, "docker-compose.yaml"), cancellation));
    }

    public string Service(string name) => services.GetValueOrDefault(name, string.Empty);

    public ValueTask DisposeAsync()
    {
        Directory.Delete(OutputDirectory, recursive: true);
        return ValueTask.CompletedTask;
    }

    private static Dictionary<string, string> ReadServices(string[] lines)
    {
        var serviceLines = lines
            .SkipWhile(line => line != ServicesKey)
            .Skip(1)
            .TakeWhile(line => line.StartsWith(ServiceIndent, StringComparison.Ordinal));

        var result = new Dictionary<string, string>();
        string? current = null;
        foreach (var line in serviceLines)
        {
            if (!line.StartsWith(ServiceIndent + " ", StringComparison.Ordinal))
            {
                current = line.Trim().TrimEnd(':');
                result[current] = string.Empty;
                continue;
            }

            result[current!] += line + Environment.NewLine;
        }

        return result;
    }
}
