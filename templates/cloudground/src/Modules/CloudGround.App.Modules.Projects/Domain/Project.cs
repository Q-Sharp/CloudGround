namespace CloudGround.App.Modules.Projects.Domain;

internal sealed class Project
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

    private Project(Guid id, string name, string? description, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private init; }

    public string Name { get; private init; }

    public string? Description { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static Project Create(string name, string? description, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), name, description, ToMicroseconds(now));

    public bool TryArchive(DateTimeOffset now)
    {
        if (ArchivedAt is not null)
        {
            return false;
        }

        ArchivedAt = ToMicroseconds(now);
        return true;
    }

    private static DateTimeOffset ToMicroseconds(DateTimeOffset time) =>
        time.AddTicks(-(time.Ticks % TimeSpan.TicksPerMicrosecond));
}
