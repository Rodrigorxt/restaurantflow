# ADR 0001: Align services with business capabilities

- Status: Accepted
- Date: 2026-08-13

## Context

The platform must scale ordering, kitchen processing, and notifications independently while keeping ownership clear.

## Decision

Use Menu, Orders, Kitchen, Payments, and Notifications as service boundaries. Each stateful service owns a PostgreSQL database. Integration contracts live in a small shared package; domain models do not.

## Consequences

The design permits independent deployment and scaling, but introduces eventual consistency and operational overhead. Local development therefore uses Docker Compose, and distributed tracing is a baseline requirement rather than a later enhancement.

