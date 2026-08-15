# Contributing

RestaurantFlow is a portfolio reference for reliable, event-driven .NET services. Contributions should preserve service ownership, backwards-compatible integration contracts, deterministic local setup, and automated verification.

## Development workflow

1. Create a focused branch from `main`.
2. Keep business behavior inside the service that owns the capability.
3. Add or update tests for observable behavior and failure paths.
4. Update architecture decisions and operational documentation when a change affects system boundaries or runtime guarantees.
5. Run the relevant local checks before opening a pull request.

```bash
dotnet restore RestaurantFlow.slnx
dotnet build RestaurantFlow.slnx --configuration Release --no-restore
dotnet test RestaurantFlow.slnx --configuration Release --no-build
docker compose config --quiet
helm lint deploy/helm/restaurantflow
terraform -chdir=infra/aws fmt -check -recursive
terraform -chdir=infra/aws validate
```

Use the full Docker Compose end-to-end workflow for changes to messaging, persistence, authentication, container images, or orchestration:

```bash
docker compose up --build --detach
./scripts/run-e2e.sh
docker compose down --volumes
```

## Pull requests

Pull requests should explain the behavior being changed, the architectural impact, the validation performed, and any deployment or migration consideration. Keep commits intentional and avoid mixing unrelated refactoring with feature work.

Never commit credentials, local Terraform state, production connection strings, generated build output, or personal environment settings.
