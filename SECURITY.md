# Security policy

## Reporting a vulnerability

Do not disclose suspected vulnerabilities in a public issue. Use GitHub's private vulnerability reporting for this repository when available, or contact the repository owner privately through the contact information on their GitHub profile.

Include the affected component, reproduction steps, potential impact, and any known mitigation. Do not include real credentials, customer data, or destructive proof-of-concept material.

## Supported versions

RestaurantFlow is a portfolio reference and supports only the latest commit on `main`. Dependencies are monitored by Dependabot, NuGet vulnerability auditing, CodeQL, container-aware integration tests, and version-pinned infrastructure validation.

Local credentials and demo identities are development-only. Production deployments must replace every example value, use an external OIDC provider and managed secrets, protect Terraform state, restrict network access, and review the production guidance in [docs/security.md](docs/security.md) and [infra/aws/README.md](infra/aws/README.md).
