resource "aws_kms_key" "data" {
  description             = "RestaurantFlow ${var.environment} data encryption"
  deletion_window_in_days = 30
  enable_key_rotation     = true
}

resource "aws_kms_alias" "data" {
  name          = "alias/${local.name}-data"
  target_key_id = aws_kms_key.data.key_id
}

resource "aws_security_group" "database" {
  name_prefix = "${local.name}-database-"
  description = "PostgreSQL access from RestaurantFlow EKS nodes"
  vpc_id      = module.vpc.vpc_id

  ingress {
    description     = "PostgreSQL from EKS nodes"
    protocol        = "tcp"
    from_port       = 5432
    to_port         = 5432
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

resource "random_password" "database" {
  for_each = local.databases

  length  = 32
  special = false
}

resource "aws_db_instance" "service" {
  for_each = local.databases

  identifier     = "${local.name}-${each.key}"
  engine         = "postgres"
  engine_version = var.database_engine_version
  instance_class = var.database_instance_class

  db_name  = each.value
  username = "restaurantflow"
  password = random_password.database[each.key].result
  port     = 5432

  allocated_storage     = 20
  max_allocated_storage = 100
  storage_type          = "gp3"
  storage_encrypted     = true
  kms_key_id            = aws_kms_key.data.arn

  db_subnet_group_name   = module.vpc.database_subnet_group_name
  vpc_security_group_ids = [aws_security_group.database.id]
  publicly_accessible    = false
  multi_az               = var.database_multi_az

  backup_retention_period   = 7
  backup_window             = "03:00-04:00"
  maintenance_window        = "Sun:04:30-Sun:05:30"
  copy_tags_to_snapshot     = true
  deletion_protection       = var.deletion_protection
  skip_final_snapshot       = false
  final_snapshot_identifier = "${local.name}-${each.key}-final"

  auto_minor_version_upgrade      = true
  performance_insights_enabled    = true
  performance_insights_kms_key_id = aws_kms_key.data.arn

  lifecycle {
    prevent_destroy = true
  }
}

