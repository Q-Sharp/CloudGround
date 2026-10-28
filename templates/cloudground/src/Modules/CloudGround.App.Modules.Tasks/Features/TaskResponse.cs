namespace CloudGround.App.Modules.Tasks.Features;

internal sealed record TaskResponse(Guid Id, Guid ProjectId, string Title, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, DateTimeOffset? ClosedAt);
