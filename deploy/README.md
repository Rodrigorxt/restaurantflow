# Kubernetes deployment

The Helm chart deploys RestaurantFlow into a dedicated namespace. It includes application Deployments and Services, RabbitMQ, isolated PostgreSQL StatefulSets, versioned database migration Jobs, persistent storage, health probes, resource boundaries, restrictive container security contexts, network policies, and horizontal autoscaling.

## Prerequisites

- Kubernetes cluster
- Helm 3
- RestaurantFlow images available to the cluster
- External OIDC authority with the expected roles and API audience

## Install or upgrade

```bash
helm upgrade --install restaurantflow ./deploy/helm/restaurantflow \
  --namespace restaurantflow \
  --create-namespace \
  --set image.repositoryPrefix=restaurantflow \
  --set image.tag=latest \
  --set authentication.authority=https://identity.example.com/realms/restaurantflow \
  --set authentication.tokenEndpoint=https://identity.example.com/realms/restaurantflow/protocol/openid-connect/token
```

Each stateful service receives a migration Job named with the Helm release revision. The Job applies pending Entity Framework Core migrations and exits. API Deployments do not run migrations during startup.

## Validate manifests

```bash
helm lint deploy/helm/restaurantflow
helm template restaurantflow deploy/helm/restaurantflow \
  --namespace restaurantflow
```

## Verify the deployment

```bash
kubectl get deployments,statefulsets,jobs,pods,services \
  --namespace restaurantflow
```

The gateway uses a `LoadBalancer` Service by default. Local clusters may require port forwarding:

```bash
kubectl port-forward service/gateway 8080:8080 \
  --namespace restaurantflow
```

## Production overrides

The included RabbitMQ and PostgreSQL resources are intended for portfolio and development environments. A production deployment should override or replace:

- image repository prefix and immutable image tag
- PostgreSQL and RabbitMQ credentials
- storage classes and volume sizes
- in-cluster databases and broker with managed or highly available services
- plain Kubernetes Secrets with an external secret provider
- OIDC authority, audience, and Orders workload credentials
- ingress, TLS, DNS, and certificate management
- backup, restore, retention, and disaster-recovery policies
- resource profiles and autoscaling thresholds based on load tests

Do not commit production credentials to `values.yaml`.
