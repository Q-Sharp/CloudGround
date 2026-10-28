using CloudGround.App.Modules.Projects.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudGround.App.Modules.Projects.Persistence;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.Property(project => project.Name).HasMaxLength(Project.NameMaxLength);
        builder.Property(project => project.Description).HasMaxLength(Project.DescriptionMaxLength);
    }
}
