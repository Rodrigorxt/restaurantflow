# RestaurantFlow

RestaurantFlow is a cloud-native restaurant ordering platform built with .NET 10. It demonstrates service boundaries, database-per-service ownership, reliable event-driven workflows, resilient HTTP communication, container orchestration, observability, and automated delivery.

The system models an order from menu selection through payment authorization, kitchen preparation, and customer notification.

## Architecture

```mermaid
flowchart LR
    Client --> Gateway[API Gateway]
    Demo[Blazor Demo] --> Gateway
    Gateway --> Menu[Menu API]
    Gateway --> Orders[Orders API]
    Gateway --> Kitchen[Kitchen API]
    Gateway --> Payments[Payments API]
    Orders -- resolve items and prices --> Menu
    Orders --> RabbitMQ
    RabbitMQ --> Orders
    RabbitMQ --> Payments
    RabbitMQ --> Kitchen
    RabbitMQ --> Notifications[Notifications Worker]
    Menu --> MenuDb[(Menu PostgreSQL)]
    Orders --> OrdersDb[(Orders PostgreSQL)]
    Payments --> PaymentsDb[(Payments PostgreSQL)]
    Kitchen --> KitchenDb[(Kitchen PostgreSQL)]
```

See [System architecture](docs/architecture.md) and the [architecture decision records](docs/decisions) for the design rationale.

## Services

| Component | Responsibility | Storage |
| --- | --- | --- |
| API Gateway | Public entry point and YARP route forwarding | None |
| Menu API | Products, categories, server-owned prices, and availability | PostgreSQL |
| Orders API | Order lifecycle, price resolution, and workflow state | PostgreSQL + transactional outbox |
| Payments API | Idempotent payment authorization simulation | PostgreSQL |
| Kitchen API | Preparation tickets and kitchen status | PostgreSQL |
| Notifications Worker | Independent customer-event processing | None |
| Blazor Demo | Recruiter-friendly executable order and workflow experience | Browser |

## Technology

- .NET 10, ASP.NET Core Minimal APIs, and Worker Services
- Blazor WebAssembly recruiter demo served through Nginx
- Entity Framework Core and PostgreSQL
- RabbitMQ and MassTransit
- Quartz.NET with clustered PostgreSQL persistence for durable workflow deadlines
- YARP API Gateway and Keycloak OIDC authentication
- OpenTelemetry traces, metrics, and logs with OTLP export
- Prometheus, Grafana, Tempo, and Loki
- Docker Compose
- Kubernetes and Helm
- Terraform, Amazon EKS, RDS, Amazon MQ, ECR, KMS, and Secrets Manager
- xUnit unit, architecture, and Testcontainers integration tests
- k6 workload scenarios with executable latency and error-rate thresholds
- GitHub Actions continuous integration

## Order workflow

1. The client submits menu item identifiers and quantities to the Orders API.
2. Orders resolves the current name, price, and availability from the Menu API.
3. Orders calculates the total, persists the aggregate, and publishes `OrderSubmitted` through its transactional outbox.
4. A persisted Orders saga sends `AuthorizePayment`, records the workflow state, and schedules a durable payment deadline.
5. Payments authorizes or declines the transaction through its transactional Inbox/Outbox.
6. An approved payment replaces the payment deadline with a durable kitchen-acceptance deadline and sends `CreateKitchenTicket`; a decline or expired deadline sends the compensating `CancelOrder` command.
7. Kitchen uses its transactional Inbox/Outbox and publishes preparation events.
8. Notifications consumes customer-relevant events independently.

Clients never provide trusted product names or prices. The Orders-to-Menu call is protected by timeout, retry, and circuit-breaker policies.

## Run locally

Requirements: Docker Desktop with Docker Compose.

```bash
docker compose up --build -d
```

Docker Compose runs a dedicated one-shot migration container for each database before starting its API. Application replicas do not modify schemas during startup.

Available endpoints:

| Resource | Address |
| --- | --- |
| API Gateway | `http://localhost:8080` |
| Live service board | `http://localhost:8082` |
| RabbitMQ Management | `http://localhost:15672` |
| RabbitMQ local credentials | `restaurantflow` / `restaurantflow` |
| Local OIDC token endpoint | `http://localhost:8081/realms/restaurantflow/protocol/openid-connect/token` |
| Grafana | `http://localhost:3000` (`admin` / `admin`) |
| Prometheus | `http://localhost:9090` |
| Tempo | `http://localhost:3200` |
| Loki | `http://localhost:3100` |

Run the requests in [docs/demo.http](docs/demo.http) in order to obtain role-specific tokens, create a menu item, submit approved and declined orders, inspect order state, and list kitchen tickets. See [Security model](docs/security.md) and [Observability](docs/observability.md).

For a visual walkthrough, open the live service board at `http://localhost:8082`, choose **Open demo service**, add menu items to the ticket, and send approved or declined orders through the real distributed workflow. The demo credentials and automatic sample menu are intentionally restricted to the local Compose environment.

Stop the environment without deleting database volumes:

```bash
docker compose down
```

## Validation

```bash
dotnet test RestaurantFlow.slnx --configuration Release
docker compose config --quiet
helm lint deploy/helm/restaurantflow
helm template restaurantflow deploy/helm/restaurantflow --namespace restaurantflow
terraform -chdir=infra/aws fmt -check -recursive
terraform -chdir=infra/aws validate
```

The GitHub Actions pipeline restores, builds, runs unit, architecture, and container-backed integration tests, validates Docker Compose, lints the Helm chart, and renders the Kubernetes manifests for every pull request. See [Testing strategy](docs/testing.md).

Run the repeatable order-submission performance baseline against the local platform with:

```bash
docker compose --profile performance run --rm k6 run /scripts/order-submission.js
```

The baseline uses a constant arrival rate, exercises real OIDC authentication and server-authoritative Menu resolution, and fails when its latency, availability, or functional thresholds are exceeded. See [Performance testing](docs/performance.md).

## Reliability and scalability

- Database-per-service ownership prevents cross-service persistence coupling.
- Orders, Payments, and Kitchen use MassTransit Entity Framework transactional Inbox/Outbox persistence.
- A PostgreSQL-backed MassTransit saga orchestrates payment, kitchen creation, cancellation compensation, and durable Quartz.NET timeouts that survive process restarts.
- Consumers use service-prefixed queues, retry policies, and idempotent business keys.
- Server-authoritative menu resolution prevents client-side price manipulation.
- Standard HTTP resilience policies protect synchronous service calls.
- Explicit migration workloads avoid concurrent schema changes across replicas.
- OpenTelemetry instruments ASP.NET Core, HTTP clients, runtime metrics, and MassTransit flows.
- Kubernetes workloads include rolling updates, probes, resource limits, restrictive security contexts, granular network policies, topology spreading, disruption budgets, and horizontal autoscaling.

## Kubernetes

The Helm chart is documented in [deploy/README.md](deploy/README.md). It provides self-contained development infrastructure and a production profile for managed PostgreSQL, managed RabbitMQ, External Secrets, TLS Ingress, migration Jobs, availability controls, network policies, and autoscaling.

## AWS infrastructure

The version-pinned [AWS Terraform reference](infra/aws/README.md) provisions a three-AZ VPC, EKS, four isolated RDS PostgreSQL databases, clustered Amazon MQ for RabbitMQ, immutable ECR repositories, KMS encryption, and a Secrets Manager document that plugs directly into the production Helm profile. It includes protected stateful resources, private endpoints, least-privilege secret access, capacity inputs, and an explicit cost warning.

## Current status

| Capability | Status |
| --- | --- |
| End-to-end order workflow | Implemented |
| Server-authoritative pricing | Implemented |
| Transactional Inbox/Outbox for Orders, Payments, and Kitchen | Implemented |
| Persisted order workflow saga and decline compensation | Implemented |
| Persisted payment and kitchen workflow deadlines | Implemented |
| Retry and circuit breaker for Menu calls | Implemented |
| Dedicated migration workloads | Implemented |
| Docker Compose and Helm deployment | Implemented |
| Automated unit and architecture tests | Implemented |
| PostgreSQL Testcontainers integration tests | Implemented for Menu pricing |
| OIDC authentication and policy authorization | Implemented |
| OAuth client credentials for Orders to Menu | Implemented |
| Full consumer idempotency coverage | Implemented for core workflow |
| Broker outage and workflow recovery validation | Implemented |
| Local observability dashboard stack | Implemented |
| RabbitMQ and full workflow integration tests | Implemented in Docker Compose CI |
| Production secret provider and managed cloud services profile | Implemented |
| Reproducible API performance baseline | Implemented |
| Interactive Blazor workflow demonstration | Implemented |
| Reproducible AWS managed infrastructure | Implemented |

## Next milestones

The portfolio baseline is complete. Future product extensions can replace the simulated payment and notification adapters, add customer-facing ordering, and introduce environment-specific GitOps promotion after cloud credentials and a target domain are available.
