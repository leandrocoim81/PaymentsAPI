variable "region" {
  type    = string
  default = "us-east-1"
}

# Learner Lab: só dev + prd (homolog documentado, não provisionado — ver specs/phase2/00-overview.md).
variable "environment" {
  type        = string
  description = "Ambiente de deploy: dev | prd."

  validation {
    condition     = contains(["dev", "prd"], var.environment)
    error_message = "environment deve ser \"dev\" ou \"prd\"."
  }
}

variable "dev_allowed_cidrs" {
  type        = list(string)
  description = "CIDRs liberados para acessar o RDS dev exposto publicamente, p/ dotnet run local."
  default     = []
}

variable "db_password" {
  type        = string
  sensitive   = true
  description = "Senha do usuário master do RDS Postgres — fornecida via TF_VAR_db_password/terraform.tfvars (nunca commitado)."
}
