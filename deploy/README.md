# Kubernetes deployment

The Helm chart deploys every application, RabbitMQ, and isolated PostgreSQL instances into a dedicated namespace. It includes rolling updates, health probes, resource boundaries, restrictive container security contexts, network policies, persistent storage, and horizontal autoscaling for traffic-sensitive workloads.

## Local cluster

```bash
helm upgrade --install restaurantflow ./deploy/helm/restaurantflow \
  --namespace restaurantflow \
  --create-namespace
```

For a local cluster, load the images built by Docker Compose into the cluster before installing the chart. In production, override `image.repositoryPrefix`, `image.tag`, storage classes, and all secret values through a secure deployment pipeline.

