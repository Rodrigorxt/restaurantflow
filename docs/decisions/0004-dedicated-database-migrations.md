# ADR 0004: Run database migrations as dedicated workloads

- Status: Accepted
- Date: 2026-08-13

## Context

Every API previously applied Entity Framework Core migrations during startup. With multiple replicas, several processes could attempt the same schema change concurrently. Application startup also mixed serving traffic with an operational deployment responsibility.

## Decision

Applications migrate only when `Database__Migrate=true`. A migration process also receives `Database__MigrationsOnly=true`, applies pending migrations, and exits without starting the web host.

Docker Compose defines a one-shot migration service for each database and makes the corresponding API depend on successful completion. The Helm chart creates a versioned Kubernetes Job for every stateful service and release revision.

## Consequences

- Application replicas do not race to update schemas.
- Migration failures are visible as deployment failures instead of unhealthy application replicas.
- Schema changes can be reviewed and operated independently from normal startup.
- Deployment automation must ensure migration Jobs complete before considering a release healthy.
- Backward-compatible expand-and-contract migrations remain necessary for zero-downtime releases.

