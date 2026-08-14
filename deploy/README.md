# Kubernetes deployment

The Helm chart deploys RestaurantFlow into a dedicated namespace. It includes application Deployments and Services, RabbitMQ, isolated PostgreSQL StatefulSets, versioned database migration Jobs, persistent storage, health probes, resource boundaries, restrictive container security contexts, granular network policies, topology spreading, disruption budgets, and horizontal autoscaling.

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

`values-production.yaml` is a documented production profile. It disables the embedded RabbitMQ and PostgreSQL resources, reads managed-service connection strings and credentials through External Secrets, enables TLS Ingress, and expects an immutable image reference.

```bash
helm upgrade --install restaurantflow ./deploy/helm/restaurantflow \
  --namespace restaurantflow \
  --create-namespace \
  --values ./deploy/helm/restaurantflow/values-production.yaml \
  --set image.repositoryPrefix=ghcr.io/acme/restaurantflow \
  --set image.tag=sha-0123456789abcdef \
  --set rabbitmq.host=rabbitmq.example.internal \
  --set ingress.host=api.restaurantflow.example.com
```

The external secret must provide `rabbitmq-username`, `rabbitmq-password`, `orders-client-secret`, and the `menu-connection`, `orders-connection`, `kitchen-connection`, and `payments-connection` keys. The External Secrets operator and referenced `SecretStore` or `ClusterSecretStore` must already exist.

Environment owners must still define and verify:

- image repository prefix and immutable image tag
- PostgreSQL and RabbitMQ credentials
- storage classes and volume sizes
- managed PostgreSQL and RabbitMQ availability, TLS, backups, and restore procedures
- OIDC authority, audience, and Orders workload credentials
- ingress, TLS, DNS, and certificate management
- backup, restore, retention, and disaster-recovery policies
- resource profiles and autoscaling thresholds based on load tests

Do not commit production credentials to `values.yaml`.
