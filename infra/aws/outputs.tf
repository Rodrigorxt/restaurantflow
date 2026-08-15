output "cluster_name" {
  description = "EKS cluster name."
  value       = module.eks.cluster_name
}

output "aws_region" {
  description = "AWS Region containing the stack."
  value       = var.aws_region
}

output "configure_kubectl_command" {
  description = "Command that configures kubectl for this EKS cluster."
  value       = "aws eks update-kubeconfig --region ${var.aws_region} --name ${module.eks.cluster_name}"
}

output "ecr_repository_urls" {
  description = "Immutable container repositories keyed by component."
  value       = { for name, repository in aws_ecr_repository.service : name => repository.repository_url }
}

output "rabbitmq_host" {
  description = "Private Amazon MQ hostname to set in rabbitmq.host."
  value       = trimsuffix(trimprefix(aws_mq_broker.rabbitmq.instances[0].endpoints[0], "amqps://"), ":5671")
}

output "application_secret_arn" {
  description = "Secrets Manager secret read by External Secrets."
  value       = aws_secretsmanager_secret.application.arn
}

output "external_secrets_policy_arn" {
  description = "IAM policy to attach to the External Secrets controller through EKS Pod Identity or IRSA."
  value       = aws_iam_policy.external_secrets.arn
}

output "helm_repository_prefix" {
  description = "Common ECR image prefix expected by the Helm chart."
  value       = "${data.aws_caller_identity.current.account_id}.dkr.ecr.${var.aws_region}.amazonaws.com/restaurantflow"
}

data "aws_caller_identity" "current" {}
