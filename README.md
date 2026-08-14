# RestaurantFlow

RestaurantFlow is a cloud-native restaurant ordering platform built to demonstrate practical microservice architecture with .NET. It models the complete journey of an order, from menu selection to payment, kitchen preparation, and customer notification.

## Architecture goals

- Keep service boundaries aligned with business capabilities.
- Use asynchronous integration events for cross-service workflows.
- Give every service ownership of its data.
- Remain observable and diagnosable across process boundaries.
- Run the same workloads with Docker Compose and Kubernetes.
- Design consumers for retries, duplicate delivery, and partial failure.

## Services

| Component | Responsibility | Storage |
| --- | --- | --- |
| API Gateway | Single public entry point and route forwarding | None |
| Menu | Products, categories, prices, and availability | PostgreSQL |
| Orders | Order lifecycle and workflow coordination | PostgreSQL |
| Kitchen | Preparation queue and kitchen status | PostgreSQL |
| Payments | Idempotent payment authorization | PostgreSQL |
| Notifications | Customer-facing event notifications | None |

## Technology

.NET 10, ASP.NET Core, Entity Framework Core, PostgreSQL, RabbitMQ, MassTransit, YARP, OpenTelemetry, Docker, Kubernetes, Helm, xUnit, and Testcontainers.

## Delivery plan

1. Establish service boundaries and shared integration contracts.
2. Implement the order workflow and persistence boundaries.
3. Add reliable asynchronous messaging and failure handling.
4. Package the platform for local container execution.
5. Add telemetry, Kubernetes resources, and deployment automation.
6. Validate architecture and behavior with automated tests.

## Run locally

Requirements: Docker Desktop with Docker Compose.

```bash
docker compose up --build -d
```

The API Gateway is available at `http://localhost:8080`. RabbitMQ Management is available at `http://localhost:15672` with the local credentials `restaurantflow` / `restaurantflow`.

Use [`docs/demo.http`](docs/demo.http) to submit approved and declined orders. The approved flow progresses from Orders to Payments and Kitchen through RabbitMQ. Kitchen endpoints then move the ticket through preparation and ready states.

```bash
dotnet test RestaurantFlow.slnx --configuration Release
helm lint deploy/helm/restaurantflow
docker compose down
```

## Reliability and scalability

- The Orders service publishes through the Entity Framework transactional outbox.
- Order names and prices are resolved from the Menu service and never trusted from client input.
- Synchronous service calls use timeout, retry, and circuit-breaker resilience policies.
- Database migrations run as explicit one-shot workloads before API containers start.
- Consumers use service-prefixed queues, retry policies, and idempotent business keys.
- Each stateful service owns an isolated PostgreSQL database.
- OpenTelemetry exports distributed traces and runtime metrics over OTLP.
- Kubernetes workloads use rolling updates, probes, resource limits, network policies, persistent volumes, and horizontal autoscaling.

## Project status

The first end-to-end order workflow is operational. Authentication, richer notification channels, and production secret management are tracked as the next delivery milestones.
