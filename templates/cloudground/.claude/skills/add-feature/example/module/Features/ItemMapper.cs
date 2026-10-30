namespace CloudGround.App.Modules.ModuleName.Features;

[Mapper]
internal static partial class ItemMapper
{
    public static partial ItemResponse ToResponse(this Item item);

    public static partial IQueryable<ItemResponse> ProjectToResponse(this IQueryable<Item> items);
}
