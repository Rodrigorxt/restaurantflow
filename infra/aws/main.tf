data "aws_availability_zones" "available" {
  state = "available"
}

locals {
  name = "restaurantflow-${var.environment}"
  azs  = slice(data.aws_availability_zones.available.names, 0, 3)

  databases = {
    menu     = "menu"
    orders   = "orders"
    kitchen  = "kitchen"
    payments = "payments"
  }

  repositories = toset([
    "gateway",
    "menu-api",
    "orders-api",
    "kitchen-api",
    "payments-api",
    "notifications-worker",
    "web"
  ])

  tags = {
    Application = "RestaurantFlow"
    Environment = var.environment
    ManagedBy   = "Terraform"
    Repository  = "github.com/Rodrigorxt/restaurantflow"
  }
}

module "vpc" {
  source  = "terraform-aws-modules/vpc/aws"
  version = "6.6.1"

  name = local.name
  cidr = "10.42.0.0/16"

  azs              = local.azs
  public_subnets   = [for index, _ in local.azs : cidrsubnet("10.42.0.0/16", 4, index)]
  private_subnets  = [for index, _ in local.azs : cidrsubnet("10.42.0.0/16", 4, index + 3)]
  database_subnets = [for index, _ in local.azs : cidrsubnet("10.42.0.0/16", 4, index + 6)]

  enable_nat_gateway     = true
  single_nat_gateway     = var.single_nat_gateway
  one_nat_gateway_per_az = !var.single_nat_gateway
  enable_dns_hostnames   = true
  enable_dns_support     = true

  create_database_subnet_group       = true
  create_database_subnet_route_table = true

  public_subnet_tags = {
    "kubernetes.io/role/elb" = "1"
  }

  private_subnet_tags = {
    "kubernetes.io/role/internal-elb" = "1"
  }
}

module "eks" {
  source  = "terraform-aws-modules/eks/aws"
  version = "21.24.0"

  name               = local.name
  kubernetes_version = var.cluster_version

  endpoint_public_access                   = true
  endpoint_private_access                  = true
  enable_cluster_creator_admin_permissions = true

  addons = {
    coredns                = { most_recent = true }
    eks-pod-identity-agent = { most_recent = true }
    kube-proxy             = { most_recent = true }
    vpc-cni                = { most_recent = true }
  }

  vpc_id     = module.vpc.vpc_id
  subnet_ids = module.vpc.private_subnets

  eks_managed_node_groups = {
    application = {
      instance_types = var.node_instance_types
      min_size       = var.node_capacity.minimum
      desired_size   = var.node_capacity.desired
      max_size       = var.node_capacity.maximum
      capacity_type  = "ON_DEMAND"

      labels = {
        workload = "application"
      }
    }
  }
}

