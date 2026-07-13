# Referências compartilhadas da plataforma (Orchestration/platform), lidas via SSM — spec 01.
# Independência: este repo nunca lê o tfstate da plataforma, só os parâmetros publicados.

data "aws_region" "current" {}
data "aws_caller_identity" "current" {}

data "aws_ssm_parameter" "vpc_id" {
  name = "/fcg/platform/vpc/id"
}

data "aws_ssm_parameter" "private_subnets" {
  name = "/fcg/platform/vpc/private_subnets"
}

# Subnets públicas (roteiam pro Internet Gateway) — o RDS "dev público" (publicly_accessible=true)
# precisa estar num subnet group público, senão o IP público fica sem rota de volta (timeout).
data "aws_ssm_parameter" "public_subnets" {
  name = "/fcg/platform/vpc/public_subnets"
}

# CIDR da VPC — usado para liberar acesso aos pods/nós do EKS no SG do RDS. Sem um SG dedicado
# do cluster publicado no SSM, liberar por CIDR da VPC é o caminho mais simples: os nós EKS só
# existem nas subnets privadas dessa VPC.
data "aws_vpc" "this" {
  id = data.aws_ssm_parameter.vpc_id.value
}

locals {
  private_subnet_ids = split(",", data.aws_ssm_parameter.private_subnets.value)
  public_subnet_ids  = split(",", data.aws_ssm_parameter.public_subnets.value)
  is_dev             = var.environment == "dev"
  vpc_cidr           = data.aws_vpc.this.cidr_block
}
