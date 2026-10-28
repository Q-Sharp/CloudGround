namespace CloudGround.App.Modules.Projects.Contracts;

public interface IProjectCatalog
{
    Task<ProjectStatus?> FindAsync(Guid projectId, CancellationToken cancellationToken);
}
