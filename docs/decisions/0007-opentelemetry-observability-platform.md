# ADR 0007: OpenTelemetry observability platform

## Status

Accepted

## Context

A distributed order crosses synchronous HTTP boundaries, a message broker, and several independently persisted consumers. Container logs alone cannot explain end-to-end latency or connect a customer request to asynchronous workflow activity.

## Decision

All .NET processes emit vendor-neutral OTLP traces, metrics, and structured logs through the shared observability building block. A central OpenTelemetry Collector performs signal routing. The local platform stores metrics in Prometheus, traces in Tempo, and logs in Loki, with Grafana provisioning the data sources and operational dashboard.

Applications know only the Collector endpoint. Storage backends can therefore be replaced by managed telemetry services without changing application instrumentation.

## Consequences

- HTTP and messaging activity can be investigated as one distributed transaction.
- Resource metadata consistently identifies the service, instance, and environment.
- Local diagnostics require additional containers and memory.
- Production environments must add access control, TLS, durable storage, retention, alerts, and cost controls.
