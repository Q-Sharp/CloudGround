namespace CloudGround.App.Modules.Projects.Features;

internal static class ProjectCache
{
    public static string Key(Guid projectId) => $"projects/{projectId}";
}
