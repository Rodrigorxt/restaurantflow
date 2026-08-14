# ADR 0002: Use asynchronous events for cross-service workflows

- Status: Accepted
- Date: 2026-08-13

## Context

Payment authorization and kitchen preparation should not make order submission depend on a long synchronous call chain.

## Decision

Publish integration events through RabbitMQ with MassTransit. Commands remain inside their owning service. Published messages describe completed facts and use past-tense names.

## Consequences

The order API responds before the distributed workflow finishes. Services must handle duplicate delivery, retries, poison messages, correlation, and eventual consistency explicitly.

