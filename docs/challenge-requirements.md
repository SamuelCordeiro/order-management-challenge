# Matriz de requisitos do desafio

Esta matriz é uma referência curta para revisões futuras. Ela foi sintetizada do documento oficial `Desafio Tecnico TMB 1.docx`; o documento original continua sendo a fonte de verdade.

## Requisitos obrigatórios

| Área | Requisito | Evidência no projeto |
| --- | --- | --- |
| API | Criar, listar e detalhar pedidos | `POST /orders`, `GET /orders` e `GET /orders/{id}` nos controllers MVC |
| Domínio | Pedido com cliente, produto, valor, status e data de criação | Agregado `Order`, contratos HTTP e migration PostgreSQL |
| Fluxo | `Pendente → Processando → Finalizado` | Métodos do agregado e testes de domínio |
| Mensageria | Publicar `OrderCreated` ao criar o pedido | Outbox transacional e contrato compartilhado |
| Contrato | `CorrelationId = OrderId` e `EventType = OrderCreated` | `OrderCreated` e testes de integração com RabbitMQ |
| Worker | Processar, aguardar cinco segundos e finalizar | `OrderProcessingWorker` |
| Confiabilidade | Consumo idempotente | Transições do agregado e histórico imutável |
| Saúde | API, PostgreSQL e RabbitMQ | `/health`, `/health/live` e health checks do Compose |
| Frontend | Tabela, criação, detalhe e feedback de status | Feature `orders` em React |
| Infraestrutura | API, Worker, frontend, banco e administração do banco com Compose | `docker-compose.yml` e pgAdmin |
| Configuração | Segredos fora do código e migrations automáticas | `.env.example`, User Secrets e aplicação de migrations no startup |

## Diferenciais implementados

- Outbox transacional para `OrderCreated` e `OrderStatusChanged`.
- Histórico de status append-only.
- SSE com polling condicional como fallback.
- Testes de integração isolados com PostgreSQL e RabbitMQ reais via Testcontainers.
- Tracing OpenTelemetry para HTTP e mensageria.
- Quality workflow com cobertura backend e frontend; a análise SonarQube Cloud é executada na branch `main`, compatível com o plano atual.

## Fora do escopo atual

- Golden tests.
- Perguntas sobre pedidos por IA/Analytics.
- Deploy hospedado. A aplicação está preparada para execução em containers; a escolha e implantação do provedor serão tratadas separadamente.
