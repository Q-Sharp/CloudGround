using CloudGround.App.Modules.Projects.Contracts;
using CloudGround.App.Modules.Projects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloudGround.App.Modules.Projects;

internal sealed class ProjectCatalog(ProjectsDbContext database) : IProjectCatalog
{
    public async Task<ProjectStatus?> FindAsync(Guid projectId, CancellationToken cancellationToken) =>
        await database.Projects
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(project => new ProjectStatus(project.Id, project.ArchivedAt != null))
            .SingleOrDefaultAsync(cancellationToken);
}
