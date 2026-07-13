# Infra própria do PaymentsAPI (spec 01 — independência dos repos).
# State próprio (S3 + lock), sem tfstate compartilhado com a plataforma ou outros serviços.
# Refs da plataforma (VPC/subnets/LabRole) são lidas via SSM em data.tf.

terraform {
  required_version = ">= 1.5"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # backend "s3" {
  #   bucket = "fcg-tfstate-<account_id>"
  #   key    = "payments-api/terraform.tfstate"
  #   region = "us-east-1"
  # }
}

provider "aws" {
  region = var.region
}
