# Spec 04 (Parte C) — RDS Postgres do PaymentsAPI.
# Código não muda (Npgsql lê ConnectionStrings:Payments via configuration.GetConnectionString("Payments")).
# dev: publicly_accessible + SG allowlist (permite dotnet run local — spec 06).
# prd: privado na VPC, só alcançável pelos pods do EKS.

resource "aws_db_subnet_group" "payments" {
  name = "fcg-${var.environment}-payments-db"
  # dev: subnets públicas (roteiam pro IGW) — publicly_accessible=true sozinho não basta.
  # prd: subnets privadas, sem rota pública.
  subnet_ids = local.is_dev ? local.public_subnet_ids : local.private_subnet_ids
}

resource "aws_security_group" "payments_db" {
  name        = "fcg-${var.environment}-payments-db-sg"
  description = "Acesso ao RDS Postgres do PaymentsAPI"
  vpc_id      = data.aws_ssm_parameter.vpc_id.value

  ingress {
    description = "Postgres a partir dos pods/nos do EKS (mesma VPC)"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = [local.vpc_cidr]
  }

  dynamic "ingress" {
    for_each = local.is_dev ? [1] : []
    content {
      description = "Postgres a partir de IPs allowlisted (dotnet run local) - so dev"
      from_port   = 5432
      to_port     = 5432
      protocol    = "tcp"
      cidr_blocks = var.dev_allowed_cidrs
    }
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_db_instance" "payments" {
  identifier     = "fcg-${var.environment}-payments"
  engine         = "postgres"
  engine_version = "16"
  instance_class = "db.t3.micro"

  allocated_storage = 20
  storage_type      = "gp3"

  db_name  = "payments_db"
  username = "postgres"
  password = var.db_password

  db_subnet_group_name   = aws_db_subnet_group.payments.name
  vpc_security_group_ids = [aws_security_group.payments_db.id]
  publicly_accessible    = local.is_dev

  multi_az            = !local.is_dev
  # Lab/desafio: sem retenção de snapshot final em nenhum ambiente (evita custo de storage
  # esquecido). Numa conta de produção real, usar false + final_snapshot_identifier em prd.
  skip_final_snapshot = true
  deletion_protection = !local.is_dev

  backup_retention_period = local.is_dev ? 0 : 7
}

output "payments_db_endpoint" {
  description = "Endpoint (host:port) do RDS — compõe ConnectionStrings__Payments no secret (spec 05)."
  value       = aws_db_instance.payments.endpoint
}
