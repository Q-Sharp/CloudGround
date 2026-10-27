# CloudGround

`dotnet new` template for container-based .NET solutions with Aspire and a visible architecture.

```bash
dotnet new install CloudGround.Templates
dotnet new cloudground --name Contoso.Shop
cd Contoso.Shop
dotnet run --project src/Contoso.Shop.AppHost
```

A generated solution contains an Aspire AppHost, shared service defaults (OpenTelemetry, health
checks, resilience, service discovery), a Minimal API host, an integration test that starts the
whole application, automatic semantic versioning from Git tags, GitHub Actions and Dependabot.

Source, roadmap and documentation: <https://github.com/Q-Sharp/CloudGround>
