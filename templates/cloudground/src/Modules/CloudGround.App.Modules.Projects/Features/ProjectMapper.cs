using CloudGround.App.Modules.Projects.Domain;
using Riok.Mapperly.Abstractions;

namespace CloudGround.App.Modules.Projects.Features;

[Mapper]
internal static partial class ProjectMapper
{
    public static partial ProjectResponse ToResponse(this Project project);

    public static partial IQueryable<ProjectResponse> ProjectToResponse(this IQueryable<Project> projects);
}
