locals {
  application_secret = merge(
    {
      "rabbitmq-username"    = "restaurantflow"
      "rabbitmq-password"    = random_password.rabbitmq.result
      "orders-client-secret" = var.orders_client_secret
    },
    {
      for name, database in local.databases :
      "${name}-connection" => "Host=${aws_db_instance.service[name].address};Port=5432;Database=${database};Username=restaurantflow;Password=${random_password.database[name].result};SSL Mode=Require;Trust Server Certificate=true"
    }
  )
}

resource "aws_secretsmanager_secret" "application" {
  name                    = "restaurantflow/${var.environment}"
  description             = "Runtime configuration consumed by RestaurantFlow through External Secrets"
  kms_key_id              = aws_kms_key.data.arn
  recovery_window_in_days = 30
}

resource "aws_secretsmanager_secret_version" "application" {
  secret_id     = aws_secretsmanager_secret.application.id
  secret_string = jsonencode(local.application_secret)
}

data "aws_iam_policy_document" "external_secrets" {
  statement {
    sid       = "ReadRestaurantFlowRuntimeSecret"
    effect    = "Allow"
    actions   = ["secretsmanager:DescribeSecret", "secretsmanager:GetSecretValue"]
    resources = [aws_secretsmanager_secret.application.arn]
  }

  statement {
    sid       = "DecryptRestaurantFlowRuntimeSecret"
    effect    = "Allow"
    actions   = ["kms:Decrypt"]
    resources = [aws_kms_key.data.arn]
  }
}

resource "aws_iam_policy" "external_secrets" {
  name        = "${local.name}-external-secrets"
  description = "Least-privilege access to the RestaurantFlow runtime secret"
  policy      = data.aws_iam_policy_document.external_secrets.json
}

