# 0005. PostgreSQL and EF Core with one schema per module

Status: Accepted

## Context

Modules own their data. A database per module costs operations; one shared schema lets modules join
each other's tables.

## Decision

One PostgreSQL database. Every module has its own `DbContext`, whose default schema is the module's
name in lowercase and whose migrations history table lives in that schema. EF Core reaches
PostgreSQL through Npgsql and Aspire's integration, which retries transient failures.

## Alternatives

- **A database per module**: the strongest isolation, but more containers, connection strings and
  backups without a gain at this size.
- **One `DbContext` for all modules**: the simplest setup, but every module sees every table, and
  each migration touches all modules.
- **SQL Server**: equally capable with EF Core; PostgreSQL is open source and runs anywhere in a
  container without license editions.
- **Dapper or plain SQL**: fast for some queries, but without migrations, change tracking or LINQ;
  EF Core is Microsoft's data access library.

## Consequences

- Cross-module reads go through contracts, never through SQL joins.
- The architecture tests check every `DbContext`'s schema and retry policy.
- Moving a module to its own database later changes only its connection.
