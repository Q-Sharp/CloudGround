# CloudGround

`dotnet new` template for container-based .NET solutions with Aspire and a visible architecture.

```bash
dotnet new install CloudGround.Templates
dotnet new cloudground --name Contoso.Shop
cd Contoso.Shop
dotnet run --project src/Contoso.Shop.AppHost
```

A generated solution contains an Aspire AppHost with PostgreSQL, Garnet, RabbitMQ and Keycloak,
shared service defaults (OpenTelemetry, health checks, resilience, service discovery), an Angular
frontend with per-route server-side rendering, a BFF that serves it, signs users in and keeps
tokens away from the browser, a modular Minimal API host with ProblemDetails, validation, OpenAPI,
HybridCache and role checks on writes, messaging with Wolverine and a transactional outbox,
*Projects* and *Tasks* sample modules that meet only through contracts, architecture tests,
integration tests that start the whole application, Playwright end-to-end tests, automatic
semantic versioning from Git tags, GitHub Actions and Dependabot.

`--sample false` leaves out the sample modules and their pages and keeps the module infrastructure,
messaging, the BFF, the login and the web app's shell.

Source, roadmap and documentation: <https://github.com/Q-Sharp/CloudGround>
