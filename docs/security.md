# Security model

RestaurantFlow uses OpenID Connect and OAuth 2.0 access tokens. Keycloak provides a reproducible local identity provider, while Kubernetes deployments are configured for an external OIDC provider.

## Authorization roles

| Role | Permissions |
| --- | --- |
| `customer` | Submit orders and read owned orders |
| `kitchen` | List, start, and complete kitchen tickets |
| `admin` | Manage Menu items, inspect Payments, and perform operational access |
| `internal` | Call private service-to-service endpoints |

The API Gateway enforces route-level policies, and each API repeats authorization at its own endpoint boundary. Public Menu reads and health checks remain anonymous.

## Customer identity

When authentication is enabled, Orders derives `CustomerId` from the JWT `sub` claim and `CustomerEmail` from the `email` claim. Values supplied in the request body are ignored. A customer can read only orders whose stored customer identifier matches the authenticated subject; administrators may inspect any order.

## Service identity

Orders uses the OAuth 2.0 client-credentials grant to call the private Menu price-resolution endpoint. A delegating handler obtains and caches a short-lived access token for the `orders-service` client. The endpoint requires the `internal` role and is not routed by the public gateway.

## Local identities

The imported development realm contains disposable local users:

| Username | Password | Role |
| --- | --- | --- |
| `customer` | `customer` | `customer` |
| `kitchen` | `kitchen` | `kitchen` |
| `restaurant-admin` | `admin` | `admin` |

These credentials and the local service secret are development-only. Run the ordered examples in [demo.http](demo.http) to obtain tokens and exercise each policy.

## Configuration

| Key | Purpose |
| --- | --- |
| `Authentication__Enabled` | Enables JWT validation and enforced role policies |
| `Authentication__Authority` | Expected OIDC issuer and discovery authority |
| `Authentication__Audience` | Required API audience |
| `Authentication__RequireHttpsMetadata` | Requires HTTPS discovery outside local development |
| `Authentication__TokenEndpoint` | OAuth token endpoint used by Orders |
| `Authentication__ClientId` | Orders machine client identifier |
| `Authentication__ClientSecret` | Orders machine credential supplied from secrets |

## Production requirements

- Use authorization code with PKCE for browser and mobile clients instead of the local direct password grant.
- Store client secrets in an external secret manager and rotate them regularly.
- Require HTTPS for discovery and all external traffic.
- Restrict issuer, audience, signing algorithms, token lifetime, and clock skew.
- Use workload identity or private-key JWT client authentication where supported.
- Record authentication failures and privileged actions without logging tokens or credentials.
- Replace local Keycloak development mode with a managed or highly available identity deployment.
