# 0008. Integration events through RabbitMQ and Wolverine's outbox

Status: Accepted

## Context

Modules react to each other's changes. An event must not get lost while the broker is down, and it
must not be sent for a change that was rolled back.

## Decision

RabbitMQ is the broker and Wolverine the messaging library. A module stores its event in Wolverine's
outbox in the same PostgreSQL transaction as the change; Wolverine sends it after the commit,
receives it through its durable inbox, retries failing handlers and then moves the message to an
error queue. Wolverine is used for messaging only; HTTP endpoints still call their handlers
directly.

## Alternatives

- **In-process events**: simple, but lost when the process stops, and they bind the reacting module
  to the same process.
- **MassTransit**: mature and widely used, but its version 9 moved to a commercial license.
- **NServiceBus**: mature, but commercial.
- **The RabbitMQ client with a hand-written outbox**: full control, but outbox, inbox, retries and
  tracing become a lot of custom code.

## Consequences

- Delivery is at least once, so every handler is idempotent.
- Wolverine needs public handlers; a module exposes them in its `Messaging` namespace and forwards
  to its internals through an interface.
- Development compiles handler code at runtime; container images load code generated during
  `dotnet publish`.
- Wolverine can switch to Azure Service Bus by configuration.
