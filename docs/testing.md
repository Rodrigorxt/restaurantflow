# Testing strategy

RestaurantFlow uses different test levels to validate business behavior, architecture boundaries, persistence, HTTP contracts, and deployment assets.

## Test suites

| Suite | Scope | External dependencies |
| --- | --- | --- |
| `RestaurantFlow.Orders.UnitTests` | Order totals, status transitions, Menu client behavior, saga commands, durable timeout behavior, late delivery, and compensation | In-memory message test harness |
| `RestaurantFlow.ArchitectureTests` | Service isolation and integration-contract dependency rules | None |
| `RestaurantFlow.Menu.IntegrationTests` | Menu HTTP endpoints, Entity Framework migrations, PostgreSQL persistence, availability, and price resolution | Ephemeral PostgreSQL container |
| `RestaurantFlow.Security.UnitTests` | Named authorization policies and role separation | None |

## Integration-test lifecycle

The Menu integration-test fixture uses Testcontainers to:

1. start a PostgreSQL 18 container with an isolated test database;
2. inject its dynamic connection string into the Menu API host;
3. apply the real Entity Framework Core migrations;
4. exercise the API through an in-memory ASP.NET Core test server;
5. dispose of the application and database container after the test class completes.

The tests do not use an in-memory database. They validate PostgreSQL-specific mappings and migration behavior against the same database engine used by the application.

## Covered pricing scenarios

- A menu item can be created and read through the public API.
- The internal pricing endpoint returns the server-owned name and price.
- An unavailable item is excluded from order price resolution.

## Run all tests

Docker must be available for the integration-test suite.

```bash
dotnet test RestaurantFlow.slnx --configuration Release
```

Run only the fast suites:

```bash
dotnet test tests/RestaurantFlow.Orders.UnitTests --configuration Release
dotnet test tests/RestaurantFlow.ArchitectureTests --configuration Release
```

Run the Menu integration tests:

```bash
dotnet test tests/RestaurantFlow.Menu.IntegrationTests --configuration Release
```

## Continuous integration

GitHub-hosted Linux runners provide Docker, so the standard test step runs all suites, including Testcontainers. Package vulnerability warnings are treated as build errors; vulnerable transitive dependencies must be upgraded or safely overridden rather than suppressed.

## Full workflow test

`scripts/run-e2e.sh` exercises the deployed system through its public gateway. It obtains real Keycloak tokens, creates server-priced menu data, verifies an approved order through PostgreSQL-backed outboxes, RabbitMQ, payment authorization, the persisted saga, and kitchen ticket creation, then verifies the declined-payment compensation path and an anonymous authorization boundary.

The failure scenarios stop Payments twice. The first outage ends before the deadline and proves durable broker delivery and workflow recovery. The second exceeds the deadline and proves that the PostgreSQL-backed Quartz schedule compensates the order. Payments is then restarted to prove that a late authorization cannot revive the cancelled order.

GitHub Actions starts the complete Docker Compose platform, runs this test, captures container diagnostics on failure, and removes all test volumes. Run it locally after `docker compose up --build -d`:

```bash
./scripts/run-e2e.sh
```
