using CloudGround.App.Modules.Tasks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudGround.App.Modules.Tasks.Persistence;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.Property(task => task.Title).HasMaxLength(TaskItem.TitleMaxLength);
        builder.HasIndex(task => task.ProjectId);
        builder.Property<uint>("Version").IsRowVersion();
    }
}
