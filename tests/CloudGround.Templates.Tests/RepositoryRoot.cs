namespace CloudGround.Templates.Tests;

public static class RepositoryRoot
{
    private const string Marker = "CloudGround.Templates.slnx";

    public static string Find()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, Marker)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"{Marker} not found above {AppContext.BaseDirectory}.");
    }
}
