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

The project is under active development. Local startup instructions will be added with the first end-to-end workflow.

