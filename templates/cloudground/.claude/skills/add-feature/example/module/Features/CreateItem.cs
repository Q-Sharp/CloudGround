using System.ComponentModel.DataAnnotations;
using CloudGround.App.Modules.ModuleName.Domain;
using CloudGround.App.Modules.ModuleName.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CloudGround.App.Modules.ModuleName.Features;

public sealed record CreateItemRequest([Required, StringLength(Item.NameMaxLength)] string Name);

internal sealed class CreateItemHandler(ModuleNameDbContext database, TimeProvider clock)
{
    public async Task<ItemResponse> HandleAsync(CreateItemRequest request, CancellationToken cancellationToken)
    {
        var item = Item.Create(request.Name, clock.GetUtcNow());
        database.Items.Add(item);
        await database.SaveChangesAsync(cancellationToken);
        return item.ToResponse();
    }
}

internal static class CreateItemEndpoint
{
    public static RouteGroupBuilder MapCreateItem(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateAsync).ProducesValidationProblem().RequireAuthorization(ModuleNamePolicies.Edit);
        return group;
    }

    private static async Task<CreatedAtRoute<ItemResponse>> CreateAsync(
        CreateItemRequest request,
        CreateItemHandler handler,
        CancellationToken cancellationToken)
    {
        var item = await handler.HandleAsync(request, cancellationToken);
        return TypedResults.CreatedAtRoute(item, GetItemEndpoint.RouteName, new { id = item.Id });
    }
}
