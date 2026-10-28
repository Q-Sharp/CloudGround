using CloudGround.App.Modules.Tasks.Domain;
using Riok.Mapperly.Abstractions;

namespace CloudGround.App.Modules.Tasks.Features;

[Mapper]
internal static partial class TaskMapper
{
    public static partial TaskResponse ToResponse(this TaskItem task);

    public static partial IQueryable<TaskResponse> ProjectToResponse(this IQueryable<TaskItem> tasks);
}
