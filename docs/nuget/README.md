# CloudGround

`dotnet new` template for container-based .NET solutions with Aspire and a visible architecture.

```bash
dotnet new install CloudGround.Templates
dotnet new cloudground --name Contoso.Shop
cd Contoso.Shop
dotnet run --project src/Contoso.Shop.AppHost
```

A generated solution contains an Aspire AppHost with PostgreSQL and Garnet, shared service
defaults (OpenTelemetry, health checks, resilience, service discovery), a modular Minimal API host
with ProblemDetails, validation, OpenAPI and HybridCache, a *Projects* sample module with EF Core
migrations and Mapperly, architecture tests, integration tests that start the whole application,
automatic semantic versioning from Git tags, GitHub Actions and Dependabot.

`--sample false` leaves out the sample module and keeps the module infrastructure.

Source, roadmap and documentation: <https://github.com/Q-Sharp/CloudGround>
