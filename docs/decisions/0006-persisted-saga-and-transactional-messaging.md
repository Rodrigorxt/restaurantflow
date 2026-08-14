# ADR 0006: Orchestrate orders with a persisted saga and transactional messaging

- Status: Accepted
- Date: 2026-08-13

## Context

The initial workflow relied on direct event choreography. Orders had a transactional outbox, but Payments and Kitchen saved business state before publishing follow-up events. A process crash between those operations could leave the order permanently incomplete. Unique indexes reduced duplicate business records but did not provide durable message-consumption state.

## Decision

Use a MassTransit state machine persisted in the Orders PostgreSQL database. `OrderSubmitted` creates a saga whose correlation identifier is the Order identifier. The saga sends explicit `AuthorizePayment`, `CreateKitchenTicket`, and compensating `CancelOrder` messages as the workflow advances.

Enable MassTransit Entity Framework Inbox and Outbox persistence in Orders, Payments, and Kitchen. Outgoing messages and business state are committed in the same database transaction. Consumer endpoints use the Entity Framework outbox middleware, retry policies, and existing unique business keys. Saga persistence uses pessimistic PostgreSQL concurrency.

Keep final saga rows as operational workflow history and expose an ownership-protected workflow endpoint.

## Consequences

- A broker outage after a database commit no longer loses core workflow messages.
- Duplicate delivery is tracked durably and business uniqueness remains enforced.
- Workflow state and failure reasons are queryable independently from the order aggregate.
- The saga becomes the explicit coordinator and introduces additional schema, queues, and operational responsibility.
- Compensation is a business action, not a distributed rollback.
- Timeout scheduling and automated recovery policies remain a further resilience enhancement.
