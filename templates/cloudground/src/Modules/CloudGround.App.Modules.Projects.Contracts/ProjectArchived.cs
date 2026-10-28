namespace CloudGround.App.Modules.Projects.Contracts;

public sealed record ProjectArchived(Guid ProjectId, DateTimeOffset ArchivedAt);
