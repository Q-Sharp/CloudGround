---
name: add-feature
description: Adds a feature slice to an existing module end to end, with endpoint, handler, validation, persistence, migration, mapping, tests and an optional page. Use when asked for a new endpoint, use case or page that belongs to a module.
---

# Add a feature

A feature slice keeps the request, the handler and the endpoint of one use case in one file under the
module's `Features/` folder. The endpoint calls the handler directly; there is no mediator. The
module must exist; create it first with the add-module skill in `.claude/skills/add-module/SKILL.md`.

`.claude/skills/add-feature/example/` holds a complete slice for an `Item` entity: creating an item
(editors only, validated) and reading it (anonymous), with a unit test and integration tests. Copy
the parts you need and rename `Item` to your entity:

| Folder | Goes to |
|---|---|
| `example/module/` | `src/Modules/CloudGround.App.Modules.<Module>/` |
| `example/module-tests/` | `tests/CloudGround.App.Modules.<Module>.Tests/` |
| `example/integration-tests/` | `tests/CloudGround.App.Integration.Tests/` |

Replace `ModuleName` with the module name and `modulename` with its lowercase form, as in the
add-module skill.

## Steps

1. **Domain.** Put the entity in `Domain/`: `internal sealed`, a private constructor, a static
   `Create` factory that takes the current time, and constants for limits such as `NameMaxLength`.
   Ids are version 7 GUIDs.
2. **Persistence.** Add an `IEntityTypeConfiguration<T>` in `Persistence/` with the limits and a
   `Version` row version, and a `DbSet` to `Persistence/<Module>DbContext.cs`:
   `public DbSet<Item> Items => Set<Item>();`.
3. **Use case.** Add one file per use case in `Features/`, named after it (`CreateItem.cs`):
   - a `public sealed record <UseCase>Request` with validation attributes such as
     `[Required, StringLength(Item.NameMaxLength)]`; .NET's built-in Minimal API validation rejects
     invalid requests with a `ProblemDetails` response before the handler runs
   - an `internal sealed class <UseCase>Handler` with `HandleAsync`, which takes the module's
     `DbContext` and, if it needs the time, `TimeProvider`
   - an `internal static class <UseCase>Endpoint` with
     `public static RouteGroupBuilder Map<UseCase>(this RouteGroupBuilder group)`; endpoints that
     change data call `.RequireAuthorization(<Module>Policies.Edit)`, reads stay anonymous, and
     handlers return `TypedResults`
4. **Mapping.** Map entities to response records with a Mapperly `[Mapper]` class in `Features/`, as
   `ItemMapper` does; a response member without a source fails the build. Map queries with a
   projection (`ProjectToResponse`) so only the needed columns are read.
5. **Registration.** In `<Module>Module.cs`, register each handler in `Add<Module>Module` with
   `builder.Services.AddScoped<<UseCase>Handler>();` and chain `.Map<UseCase>()` after
   `.WithTags("<Module>")` in `Map<Module>Module`.
6. **Migration.** Build, then add a migration with the pinned `dotnet ef` from the solution root:

   ```bash
   dotnet build
   dotnet tool restore
   ConnectionStrings__database="Host=localhost" dotnet ef migrations add <Name> \
     --project src/Modules/CloudGround.App.Modules.<Module> --startup-project src/CloudGround.App.Api \
     --context <Module>DbContext --output-dir Persistence/Migrations
   ```

   Leave the generated files as they are. The AppHost applies the migration before the API starts.
7. **Tests.** Write them first and watch them fail:
   - unit tests of the domain and of handler logic in the module's test project
   - integration tests through the API in `tests/CloudGround.App.Integration.Tests`, with
     `fixture.CreateApiClientAsync("alice")` for an editor, `"bob"` for a signed-in user without
     roles and `fixture.CreateApiClient()` for an anonymous caller; cover the success path, the role
     check and validation
   - an end-to-end test in `tests/CloudGround.App.EndToEnd.Tests` only for a page
8. **Page (optional).** Follow the Angular rules in `AGENTS.md`. A page is a standalone component in
   its own folder under `src/CloudGround.App.Web/src/app/`, like
   `src/CloudGround.App.Web/src/app/home/home.ts`. Add its route to
   `src/CloudGround.App.Web/src/app/app.routes.ts` and its render mode to
   `src/CloudGround.App.Web/src/app/app.routes.server.ts`: `RenderMode.Server` for public pages
   with data, `RenderMode.Client` for pages behind the login. Call the API with relative
   `/api/<module>` URLs through `HttpClient`, and put the component's unit test next to it in a
   `.spec.ts` file.
9. **Verify.**

   ```bash
   dotnet build
   dotnet format --verify-no-changes
   dotnet test --solution CloudGround.App.slnx
   ```

   For a page, also run `npm run build` and `npm test` in `src/CloudGround.App.Web`.

## Related

- Caching a read path with `HybridCache`: "Architecture" in `README.md`.
- Reacting to another module through an integration event: "Add an integration event" in
  `README.md`.
