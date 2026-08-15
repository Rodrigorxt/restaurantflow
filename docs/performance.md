# Performance testing

RestaurantFlow includes a k6 baseline for the synchronous order-acceptance path. It measures the part of the workflow that directly affects client response time: OIDC validation, Gateway routing, current Menu data resolution, total calculation, Orders persistence, and transactional outbox creation.

## Workload model

The default local profile uses a constant arrival rate of five order submissions per second for 30 seconds. Preallocated virtual users reduce load-generator startup noise, and k6 may scale up to the configured maximum to maintain the requested rate.

| Setting | Default | Environment variable |
| --- | ---: | --- |
| Arrival rate | 5 requests/second | `RATE` |
| Duration | 30 seconds | `DURATION` |
| Preallocated virtual users | 10 | `PRE_ALLOCATED_VUS` |
| Maximum virtual users | 30 | `MAX_VUS` |

The baseline is intentionally modest enough for a developer laptop. Increase the rate only after recording the host, Docker resources, image versions, database state, and test commit so results remain comparable.

## Service-level objectives

- At least 99% of order submissions complete without an HTTP error.
- At least 99% of functional checks pass.
- 95% of order-submission responses complete within 1.5 seconds.
- 99% complete within 2.5 seconds.
- No scheduled iterations are dropped at the default rate.

These are portfolio baselines, not production capacity claims. Production targets require representative infrastructure, data volume, network latency, and an agreed business traffic model.

## Reference run

A Docker Desktop run on 2026-08-15 at the default five orders per second produced the following result. It is evidence that the scenario and thresholds execute successfully, not a cross-environment benchmark.

| Result | Value |
| --- | ---: |
| Accepted orders | 151 |
| Failed requests | 0% |
| Dropped iterations | 0 |
| Submission p95 | 98.45 ms |
| Submission p99 | 185.28 ms |

## Run locally

Start the platform, then run the isolated Compose performance profile:

```bash
docker compose up --build -d
docker compose --profile performance run --rm k6 run /scripts/order-submission.js
```

Override the workload without editing the test:

```bash
docker compose --profile performance run --rm \
  -e RATE=20 \
  -e DURATION=2m \
  -e PRE_ALLOCATED_VUS=30 \
  -e MAX_VUS=100 \
  k6 run /scripts/order-submission.js
```

The process exits non-zero when any threshold fails, which makes the same scenario suitable for a scheduled performance pipeline. It is kept out of pull-request CI because shared runners do not provide stable capacity measurements.

## Reading results

Compare `order_submission_duration`, `order_submission_failures`, `checks`, and `dropped_iterations` first. A rise in end-to-end duration with stable Gateway latency usually points to Menu or database work. Correlate the test window in Grafana with ASP.NET Core, outbound HTTP, MassTransit, PostgreSQL, and runtime telemetry before changing limits or scaling replicas.
