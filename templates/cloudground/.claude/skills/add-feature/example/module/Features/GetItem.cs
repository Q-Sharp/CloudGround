using CloudGround.App.Modules.ModuleName.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CloudGround.App.Modules.ModuleName.Features;

internal sealed class GetItemHandler(ModuleNameDbContext database)
{
    public Task<ItemResponse?> HandleAsync(Guid itemId, CancellationToken cancellationToken) =>
        database.Items
            .AsNoTracking()
            .Where(item => item.Id == itemId)
            .ProjectToResponse()
            .SingleOrDefaultAsync(cancellationToken);
}

internal static class GetItemEndpoint
{
    public const string RouteName = "GetItem";

    public static RouteGroupBuilder MapGetItem(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", GetAsync).WithName(RouteName);
        return group;
    }

    private static async Task<Results<Ok<ItemResponse>, NotFound>> GetAsync(
        Guid id,
        GetItemHandler handler,
        CancellationToken cancellationToken) =>
        await handler.HandleAsync(id, cancellationToken) is { } item
            ? TypedResults.Ok(item)
            : TypedResults.NotFound();
}
