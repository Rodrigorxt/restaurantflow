# ADR 0008: Production Kubernetes profile

## Status

Accepted

## Context

The self-contained chart is useful for demonstrations, but single-replica databases, an embedded broker, plaintext values, mutable images, and unrestricted workload identities are unsuitable production defaults.

## Decision

The chart keeps its self-contained developer profile and adds a production values profile. Production disables embedded stateful infrastructure, consumes managed database and broker endpoints, materializes credentials with External Secrets, exposes only the gateway through TLS Ingress, and requires immutable image references.

Application workloads use a dedicated service account without automatic API credentials, restrictive container contexts, topology spread constraints, disruption budgets, health probes, resource boundaries, granular ingress policies, and horizontal autoscaling. The chart includes a JSON schema and CI renders both profiles.

## Consequences

- The same chart supports reproducible demonstrations and realistic cloud integration.
- Production installation requires an External Secrets operator, a configured secret store, managed PostgreSQL databases, managed RabbitMQ, an ingress controller, and TLS certificate management.
- Availability targets, sizing, backups, and disaster recovery remain environment-specific operational responsibilities.
