namespace CloudGround.App.Modules.ModuleName.Persistence;

internal sealed class ModuleNameDbContext(DbContextOptions<ModuleNameDbContext> options) : DbContext(options)
{
    public const string Schema = "modulename";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ModuleNameDbContext).Assembly);
    }
}
