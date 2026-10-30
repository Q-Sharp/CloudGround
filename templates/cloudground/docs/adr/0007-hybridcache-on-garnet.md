# 0007. HybridCache on Garnet

Status: Accepted

## Context

Read paths should be cached, and the cache must stay correct with several API instances. The API
also needs a shared store for sessions and Data Protection keys.

## Decision

`HybridCache` uses Garnet as its distributed cache, reached through the Redis client and Aspire's
integration. Cached reads skip the in-process level, so an entry that one instance removes is gone
for all of them. The API keeps session tickets and Data Protection keys in Garnet.

## Alternatives

- **Redis**: the reference server for the protocol; its license changed in 2024, while Garnet is
  Microsoft's server for the same protocol under the MIT license.
- **Valkey**: an open-source fork of Redis and equally compatible; Garnet follows the
  Microsoft-first rule.
- **`IMemoryCache` only**: no shared state, so sessions and cache invalidation break as soon as two
  instances run.
- **`IDistributedCache` directly**: no protection against cache stampedes; `HybridCache` adds it.

## Consequences

- Any server that speaks the Redis protocol can replace Garnet without code changes.
- Each cached read costs a network round trip, in exchange for correctness across instances.
