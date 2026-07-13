# Spec 05 — Segredos do PaymentsAPI no AWS Secrets Manager.
# O ESO (instalado pela plataforma — spec 01) sincroniza estes secrets para o namespace fcg-<env>
# via ExternalSecret (spec 07/k8s-<env>.yaml). Nenhum valor sensível fica no código/tfstate versionado.
#
# fcg/<env>/common/jwt NÃO é criado aqui — é o UsersAPI (emissor dos tokens) quem o cria
# (UsersAPI/infra/secretsmanager.tf). O PaymentsAPI só consome esse secret via ExternalSecret pelo nome.

resource "aws_secretsmanager_secret" "payments_db" {
  name = "fcg/${var.environment}/payments/db"
}

resource "aws_secretsmanager_secret_version" "payments_db" {
  secret_id = aws_secretsmanager_secret.payments_db.id
  secret_string = jsonencode({
    ConnectionStrings__Payments = "Host=${split(":", aws_db_instance.payments.endpoint)[0]};Port=5432;Database=payments_db;Username=postgres;Password=${var.db_password}"
  })
}
