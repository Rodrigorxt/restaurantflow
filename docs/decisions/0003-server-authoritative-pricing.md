# ADR 0003: Resolve server-authoritative menu snapshots during order submission

- Status: Accepted
- Date: 2026-08-13

## Context

An order request originally included product names and unit prices supplied by the client. Trusting those fields would allow stale data or price manipulation and would make the Orders service persist unverifiable commercial values.

Orders needs the current Menu price and availability when accepting an order. Reading the Menu database would violate service ownership, while an eventually consistent price projection would permit a window of stale pricing without additional versioning rules.

## Decision

Order requests contain only menu item identifiers and quantities. During submission, Orders calls an internal Menu endpoint to resolve the current name, price, and availability. Orders stores the returned values as immutable order-line snapshots and calculates the total on the server.

The HTTP client uses standard timeout, retry, and circuit-breaker resilience policies. Orders rejects the request when an item is missing or unavailable and does not accept orders while commercial data cannot be verified.

## Consequences

- Clients cannot control persisted names or prices.
- Historical orders retain the values accepted at purchase time even if Menu changes later.
- Order submission has a synchronous dependency on Menu availability.
- Resilience policies reduce transient failures but cannot remove the dependency.
- A future high-throughput design may replace the lookup with a versioned local Menu projection and explicit price-version validation.

