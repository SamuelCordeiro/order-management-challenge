# Order Management Challenge

Sistema de gestão de pedidos construído para o desafio técnico, com API .NET 9, React, PostgreSQL, RabbitMQ e Docker Compose.

## Executar

**Pré-requisito:** Docker Desktop em execução.

Na raiz do repositório, execute um único comando:

```powershell
docker compose --env-file .env.example up --build
```

O comando inicia frontend, API, Worker, PostgreSQL, RabbitMQ e pgAdmin, aplica as migrations automaticamente e mantém todas as portas restritas a `localhost`.

Após a inicialização, acesse:

- Aplicação: http://localhost:3000/orders
- Swagger: http://localhost:8080/swagger
- RabbitMQ Management: http://localhost:15672
- pgAdmin: http://localhost:5050

As credenciais locais de demonstração estão em [.env.example](.env.example). Para encerrar sem apagar os dados, use `docker compose down`.

## Fluxo principal

1. A API cria o pedido como `Pendente` e grava `OrderCreated` na outbox na mesma transação.
2. O publicador entrega o evento ao RabbitMQ com confirmação do broker.
3. O Worker processa o pedido: `Pendente → Processando → Finalizado`, com cinco segundos entre as transições.
4. O histórico é imutável, e o consumidor trata reentregas sem repetir efeitos.
5. O frontend recebe atualizações por SSE e usa polling apenas como fallback para pedidos em andamento.

## API

- `POST /orders` cria um pedido.
- `GET /orders` lista pedidos.
- `GET /orders/{id}` consulta um pedido.
- `GET /orders/{id}/history` consulta o histórico de status.
- `GET /health` verifica API, PostgreSQL e RabbitMQ.
- `GET /health/live` verifica somente o processo.

Exemplo de criação:

```json
{
  "cliente": "Ana Silva",
  "produto": "Notebook",
  "valor": 4999.90
}
```

## Validar

A collection do Postman inclui todos os endpoints e um cenário de reentrega idempotente: [Order Management Challenge](docs/postman/order-management-challenge.postman_collection.json).

Para verificar a suíte automatizada:

```powershell
dotnet test src/OrderManagement.sln

Push-Location frontend
npm ci
npm run lint
npm run test
npm run build
Pop-Location
```

Os testes de integração executam contra PostgreSQL e RabbitMQ reais e efêmeros via Testcontainers. O workflow de qualidade do GitHub valida build, testes, lint e cobertura.

## Decisões técnicas

- **Monólito modular:** separa domínio, casos de uso, infraestrutura e adaptadores HTTP sem introduzir complexidade de microserviços.
- **Transactional Outbox:** mantém estado e evento na mesma transação, reduzindo o risco entre persistência e publicação.
- **Entrega at-least-once + idempotência:** ACK manual, retry, DLQ e regras de transição no domínio tornam reentregas seguras.
- **SSE com fallback:** atualização leve em tempo real sem exigir WebSocket; polling condicional preserva o funcionamento em caso de desconexão.
- **Containers locais:** a pilha completa é reproduzível com Compose, com health checks e migrations no startup.

## Documentação

- [Arquitetura e fluxo confiável](docs/architecture.md)
- [Matriz de requisitos e evidências](docs/challenge-requirements.md)
- [Collection Postman](docs/postman/order-management-challenge.postman_collection.json)
