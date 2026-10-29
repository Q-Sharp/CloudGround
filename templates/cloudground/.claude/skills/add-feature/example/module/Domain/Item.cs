namespace CloudGround.App.Modules.ModuleName.Domain;

internal sealed class Item
{
    public const int NameMaxLength = 100;

    private Item(Guid id, string name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private init; }

    public string Name { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public static Item Create(string name, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), name, ToMicroseconds(now));

    private static DateTimeOffset ToMicroseconds(DateTimeOffset time) =>
        time.AddTicks(-(time.Ticks % TimeSpan.TicksPerMicrosecond));
}
