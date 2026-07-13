# PaymentsAPI

Microsserviço responsável por simular o processamento de pagamentos na plataforma FiapCloudGames.

## Responsabilidades
- Consumir `OrderPlacedEvent` via Amazon SQS/SNS (MassTransit)
- Simular aprovação/rejeição do pagamento (90% aprovação)
- Publicar `PaymentProcessedEvent` com o resultado

## Fluxo
```
OrderPlacedEvent (consumido)
       ↓
Simula pagamento (Approved / Rejected)
       ↓
PaymentProcessedEvent (publicado)
```

## Eventos consumidos
| Evento | Ação |
|--------|------|
| `OrderPlacedEvent` | Processa o pagamento e publica resultado |

## Eventos publicados
| Evento | Quando |
|--------|--------|
| `PaymentProcessedEvent` | Após simulação do pagamento |

## Variáveis de Ambiente

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__Payments` | Connection string do RDS Postgres (event store) |
| `Jwt__RsaPublicKey` | Chave pública RSA (base64 PEM) usada para validar o JWT (JWKS da UsersAPI) |
| `Jwt__Issuer` | Issuer do JWT |
| `Jwt__Audience` | Audience do JWT |
| `AWS__Region` | Região AWS (filas SQS/SNS) |
| `Messaging__Scope` | Prefixo de isolamento por ambiente das filas/tópicos SQS/SNS (ex.: `fcg-dev`) |

Segredos (JWT, RDS) são injetados em runtime via AWS Secrets Manager + External Secrets Operator no
EKS (spec 05); localmente, via `dotnet user-secrets` (spec 06).

## Executando localmente

Pré-requisitos: **.NET 8 SDK** + credenciais AWS com acesso aos recursos do ambiente `dev`. Sem
docker-compose, sem Kubernetes:

```bash
dotnet run --project src/Payments.API
```

## Health Check

```
GET /health
```
