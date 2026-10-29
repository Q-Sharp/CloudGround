# Architecture decision records

Each record describes one significant decision behind this solution: the situation, what was
decided, the alternatives and why they lost, and what follows from it. A decision that changes gets
a new record that supersedes the old one.

| ADR | Decision | Status |
|---|---|---|
| 0001 | [Modular monolith with feature slices](0001-modular-monolith.md) | Accepted |
| 0002 | [Minimal APIs with handlers called directly](0002-minimal-apis.md) | Accepted |
| 0003 | [Aspire for orchestration](0003-aspire.md) | Accepted |
| 0004 | [Microsoft libraries first](0004-microsoft-libraries-first.md) | Accepted |
| 0005 | [PostgreSQL and EF Core with one schema per module](0005-postgresql-schema-per-module.md) | Accepted |
| 0006 | [Migrations through Aspire and migration bundles](0006-migrations-through-aspire.md) | Accepted |
| 0007 | [HybridCache on Garnet](0007-hybridcache-on-garnet.md) | Accepted |
| 0008 | [Integration events through RabbitMQ and Wolverine's outbox](0008-rabbitmq-and-wolverine.md) | Accepted |
| 0009 | [Backend for frontend with cookie sessions and YARP](0009-backend-for-frontend.md) | Accepted |
| 0010 | [Keycloak locally, any OpenID Connect provider in operation](0010-keycloak-and-openid-connect.md) | Accepted |
| 0011 | [Angular with server-side rendering per route](0011-angular-with-server-side-rendering.md) | Accepted |
| 0012 | [Mapperly for object mapping](0012-mapperly.md) | Accepted |
| 0013 | [Architecture tests with ArchUnitNET](0013-architecture-tests.md) | Accepted |
| 0014 | [xUnit v3, Aspire.Hosting.Testing and Playwright](0014-test-stack.md) | Accepted |
| 0015 | [OpenTelemetry for logs, traces and metrics](0015-opentelemetry.md) | Accepted |
| 0016 | [Container images from the .NET SDK on Ubuntu chiseled images](0016-container-images.md) | Accepted |
| 0017 | [Docker Compose from Aspire's publish pipeline](0017-docker-compose-deployment.md) | Accepted |
| 0018 | [Versions from Conventional Commits, release-please and MinVer](0018-versioning.md) | Accepted |
| 0019 | [Newest LTS versions and Dependabot](0019-version-policy.md) | Accepted |
| 0020 | [Devcontainer with its own Docker daemon](0020-devcontainer.md) | Accepted |
| 0021 | [Rules, skills and tools for AI agents in the repository](0021-ai-collaboration-kit.md) | Accepted |
