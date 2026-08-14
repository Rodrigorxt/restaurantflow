# ADR 0005: Use OIDC identities and policy-based authorization

- Status: Accepted
- Date: 2026-08-13

## Context

Public endpoints originally accepted requests without an authenticated identity. Customer identifiers and email addresses were supplied by clients, privileged Menu and Kitchen operations were unrestricted, and the Orders-to-Menu call had no machine identity.

## Decision

Use OpenID Connect discovery and OAuth 2.0 JWT bearer tokens with a required API audience. Centralize token validation and named role policies in a shared security building block. Enforce policies at both the gateway route and API endpoint boundaries.

Use `customer`, `kitchen`, `admin`, and `internal` roles. Derive order ownership from token claims and authorize reads against the stored customer identifier. Use the client-credentials grant for Orders-to-Menu communication.

Keycloak supplies reproducible local identities. Production Helm values target an external OIDC authority and obtain confidential credentials from a Kubernetes Secret that should be replaced by an external secret provider.

## Consequences

- Client-supplied identity fields cannot impersonate another customer when authentication is enabled.
- Compromising the gateway alone does not bypass API endpoint authorization.
- Internal HTTP dependencies require token acquisition, caching, rotation, and failure handling.
- Local development gains additional startup time and identity infrastructure.
- Browser clients require an authorization-code-with-PKCE flow that is outside the current backend scope.
