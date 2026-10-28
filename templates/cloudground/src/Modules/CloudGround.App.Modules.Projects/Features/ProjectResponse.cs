namespace CloudGround.App.Modules.Projects.Features;

internal sealed record ProjectResponse(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt, DateTimeOffset? ArchivedAt);
