# ADR 0010: AWS managed infrastructure reference

## Status

Accepted

## Context

The production Helm profile defines secure runtime expectations but does not prove how the required network, compute, persistence, messaging, registry, and secret services fit together in a public cloud. A portfolio deployment also needs a reproducible plan rather than console-created resources.

## Decision

Provide a version-pinned Terraform reference for AWS. Amazon EKS runs the workloads across three Availability Zones. Each stateful service owns a separate private Amazon RDS for PostgreSQL instance. A private Amazon MQ RabbitMQ cluster carries integration messages. Amazon ECR stores immutable images, and AWS Secrets Manager exposes one document matching the Helm External Secret contract.

Customer-managed KMS keys encrypt stateful resources. Security groups grant database and broker access only from EKS nodes. The External Secrets controller receives a least-privilege IAM policy and should use EKS Pod Identity or IRSA. Terraform state must use an encrypted, access-controlled S3 backend because generated credentials are state values.

Application infrastructure does not install shared platform controllers such as ingress, certificate management, External Secrets, DNS, or the external identity provider. Those components have independent lifecycles and should be owned by the platform boundary.

## Consequences

- Reviewers can validate and plan a concrete multi-AZ cloud topology from source control.
- Database-per-service ownership is preserved in production instead of collapsing services into one shared database.
- Managed backups, encryption, deletion protection, immutable registries, and private endpoints improve the production security and recovery posture.
- The production defaults are intentionally expensive and must be cost-reviewed before apply.
- Region-specific Kubernetes, PostgreSQL, RabbitMQ, and instance versions remain explicit deployment inputs.

