namespace CloudGround.App.Modules.Tasks.Domain;

internal sealed class TaskItem
{
    public const int TitleMaxLength = 200;

    private TaskItem(Guid id, Guid projectId, string title, DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        Title = title;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private init; }

    public Guid ProjectId { get; private init; }

    public string Title { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    private bool IsOpen => CompletedAt is null && ClosedAt is null;

    public static TaskItem Create(Guid projectId, string title, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), projectId, title, ToMicroseconds(now));

    public bool TryComplete(DateTimeOffset now)
    {
        if (!IsOpen)
        {
            return false;
        }

        CompletedAt = ToMicroseconds(now);
        return true;
    }

    public void Close(DateTimeOffset now)
    {
        if (IsOpen)
        {
            ClosedAt = ToMicroseconds(now);
        }
    }

    private static DateTimeOffset ToMicroseconds(DateTimeOffset time) =>
        time.AddTicks(-(time.Ticks % TimeSpan.TicksPerMicrosecond));
}
