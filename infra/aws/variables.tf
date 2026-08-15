variable "aws_region" {
  description = "AWS Region used by all RestaurantFlow resources."
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Deployment environment name."
  type        = string
  default     = "production"

  validation {
    condition     = can(regex("^[a-z][a-z0-9-]{1,15}$", var.environment))
    error_message = "The environment must be a lowercase DNS-compatible name between 2 and 16 characters."
  }
}

variable "cluster_version" {
  description = "Kubernetes control-plane version supported by EKS in the target Region."
  type        = string
  default     = "1.35"
}

variable "node_instance_types" {
  description = "EC2 instance types used by the managed node group."
  type        = list(string)
  default     = ["m7i.large"]
}

variable "node_capacity" {
  description = "Managed node group autoscaling boundaries."
  type = object({
    minimum = number
    desired = number
    maximum = number
  })
  default = {
    minimum = 2
    desired = 3
    maximum = 6
  }
}

variable "database_instance_class" {
  description = "RDS instance class allocated to each service-owned database."
  type        = string
  default     = "db.t4g.micro"
}

variable "database_engine_version" {
  description = "PostgreSQL major version available in the target Region."
  type        = string
  default     = "17"
}

variable "database_multi_az" {
  description = "Create synchronous RDS standby instances in another Availability Zone."
  type        = bool
  default     = true
}

variable "rabbitmq_instance_type" {
  description = "Amazon MQ broker instance type."
  type        = string
  default     = "mq.m5.large"
}

variable "rabbitmq_engine_version" {
  description = "RabbitMQ engine version supported by Amazon MQ."
  type        = string
  default     = "4.2"
}

variable "single_nat_gateway" {
  description = "Use one NAT gateway to reduce portfolio/demo cost. Disable for per-AZ egress resilience."
  type        = bool
  default     = true
}

variable "deletion_protection" {
  description = "Protect stateful production resources from accidental deletion."
  type        = bool
  default     = true
}

variable "orders_client_secret" {
  description = "OIDC client secret used by Orders API. Supply through TF_VAR_orders_client_secret."
  type        = string
  sensitive   = true
}

