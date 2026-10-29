using CloudGround.App.Modules.ModuleName.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudGround.App.Modules.ModuleName.Persistence;

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.Property(item => item.Name).HasMaxLength(Item.NameMaxLength);
        builder.Property<uint>("Version").IsRowVersion();
    }
}
