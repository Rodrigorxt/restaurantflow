# ADR 0009: Durable workflow deadlines

## Status

Accepted

## Context

An order can remain indefinitely in payment authorization or kitchen acceptance when a downstream service is unavailable. In-memory timers disappear during a restart and cannot coordinate safely across multiple Orders replicas.

## Decision

Use MassTransit scheduled saga events backed by a clustered Quartz.NET scheduler. Store Quartz jobs and triggers in the Orders PostgreSQL database, manage its schema through the existing Orders Entity Framework migration workload, and use a database-qualified table prefix.

The workflow keeps one schedule token. Progress unschedules the current deadline before scheduling the next stage. An expired payment or kitchen-acceptance deadline publishes the existing `CancelOrder` compensation and finalizes the saga. State-specific ignores and aggregate status guards make late and duplicate delivery harmless.

Timeouts are configuration values. Docker Compose uses short values for executable failure demonstrations, while Helm and application defaults use production-oriented values.

## Consequences

- Deadlines survive process and broker restarts.
- Multiple Orders replicas coordinate scheduler ownership through PostgreSQL clustering.
- Deployment migrations must include both domain and Quartz schema changes.
- PostgreSQL is now part of the scheduling availability path.
- Operators can tune each deadline without rebuilding an image.
