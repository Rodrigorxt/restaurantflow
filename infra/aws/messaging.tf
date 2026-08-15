resource "aws_security_group" "rabbitmq" {
  name_prefix = "${local.name}-rabbitmq-"
  description = "AMQPS access from RestaurantFlow EKS nodes"
  vpc_id      = module.vpc.vpc_id

  ingress {
    description     = "AMQPS from EKS nodes"
    protocol        = "tcp"
    from_port       = 5671
    to_port         = 5671
    security_groups = [module.eks.node_security_group_id]
  }

  egress {
    protocol    = "-1"
    from_port   = 0
    to_port     = 0
    cidr_blocks = ["0.0.0.0/0"]
  }

  lifecycle {
    create_before_destroy = true
  }
}

resource "random_password" "rabbitmq" {
  length  = 32
  special = false
}

resource "aws_mq_broker" "rabbitmq" {
  broker_name = local.name

  engine_type        = "RABBITMQ"
  engine_version     = var.rabbitmq_engine_version
  host_instance_type = var.rabbitmq_instance_type
  deployment_mode    = "CLUSTER_MULTI_AZ"

  publicly_accessible = false
  subnet_ids          = module.vpc.private_subnets
  security_groups     = [aws_security_group.rabbitmq.id]

  auto_minor_version_upgrade = true
  apply_immediately          = false

  encryption_options {
    use_aws_owned_key = false
    kms_key_id        = aws_kms_key.data.arn
  }

  logs {
    general = true
  }

  user {
    username = "restaurantflow"
    password = random_password.rabbitmq.result
  }
}

