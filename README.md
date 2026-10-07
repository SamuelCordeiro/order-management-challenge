# Order Management Challenge

Sistema de gestão de pedidos desenvolvido como desafio técnico, com .NET, React, PostgreSQL, RabbitMQ e Docker.

## Estado atual

O incremento `feature/realtime-status` adiciona histórico imutável de status e atualização em tempo real. A API publica `OrderCreated` após persistir um pedido; o Worker o processa de forma assíncrona e emite `OrderStatusChanged`; a interface consome SSE, com polling como fallback.

## Executar a API

### IIS Express ou dotnet run

1. Configure uma conexão PostgreSQL local com User Secrets. Os valores ficam fora do repositório:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:OrdersDatabase" "Host=localhost;Port=5432;Database=orders;Username=postgres;Password=sua-senha" --project src/OrderManagement.Api
   ```

   Para publicar eventos fora do Compose, configure também o RabbitMQ local:

   ```powershell
   dotnet user-secrets set "RabbitMq:Host" "localhost" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:Username" "orders" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:Password" "sua-senha" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:Exchange" "order.events" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:Queue" "order.created.v1" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:RoutingKey" "order.created.v1" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:ErrorExchange" "order.events.error" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:ErrorQueue" "order.created.v1.error" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:ErrorRoutingKey" "order.created.v1.error" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:RetryExchange" "order.events.retry" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:RetryQueue" "order.created.v1.retry" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:RetryRoutingKey" "order.created.v1.retry" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:StatusExchange" "order.status.events" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:StatusQueue" "order.status.changed.v1" --project src/OrderManagement.Api
   dotnet user-secrets set "RabbitMq:StatusRoutingKey" "order.status.changed.v1" --project src/OrderManagement.Api
   ```

2. No Visual Studio, selecione o perfil **IIS Express** e execute. O navegador abrirá em `https://localhost:44395/swagger`.

   Pelo terminal, também é possível usar:

   ```powershell
   dotnet run --project src/OrderManagement.Api
   ```

As migrations EF Core são aplicadas no startup. Para criar uma nova migration, defina a mesma variável e execute:

```powershell
dotnet ef migrations add NomeDaMigration --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.Api --output-dir Persistence/Migrations
```

### Docker Compose

1. Copie `.env.example` para `.env` e defina uma senha local para `POSTGRES_PASSWORD`.
2. Suba o ambiente completo — frontend, API, PostgreSQL, RabbitMQ e Worker:

   ```powershell
   docker compose up --build
   ```

O Compose aguarda PostgreSQL e RabbitMQ ficarem saudáveis antes de iniciar API e Worker, e a API antes do frontend. As portas publicadas ficam restritas a `localhost`: aplicação em `http://localhost:3000`, Swagger em `http://localhost:8080/swagger` e Management UI em `http://localhost:15672` (credenciais `RABBITMQ_USER` e `RABBITMQ_PASSWORD`). Para encerrar mantendo os dados, execute `docker compose down`.

`/health` da API e do Worker valida PostgreSQL e RabbitMQ; `/health/live` valida somente que o processo está ativo. O endpoint do Worker fica interno ao Compose e é usado pelo health check do container.

### Testes

Os testes de domínio protegem a criação, a sequência obrigatória de status e o histórico imutável. O teste de aplicação protege o limite transacional do caso de uso: o pedido é persistido antes de `OrderCreated` ser publicado. Os testes de integração sobem PostgreSQL e RabbitMQ efêmeros com Testcontainers e verificam a criação HTTP, a persistência do histórico e o contrato publicado no broker. No frontend, os testes cobrem formulário, formatação, ordenação, apresentação de status e o contrato SSE.

```powershell
dotnet test src/OrderManagement.sln --no-restore
```

Para gerar cobertura no formato consumido pelo CI e pelo SonarQube Cloud:

```powershell
dotnet test src/OrderManagement.sln --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
```

Os testes de frontend cobrem a submissão e a validação do formulário, além da apresentação dos status:

```powershell
cd frontend
npm ci
npm run test
npm run test:coverage
npm run build
```

### Qualidade contínua e SonarQube Cloud

O workflow `Quality` executa build, testes de integração, lint, testes de frontend e publica os relatórios de cobertura em pull requests para `develop` e `main`. A análise SonarQube Cloud permanece opcional até o projeto ser conectado, evitando segredos ou identificadores externos no código.

Para ativá-la, crie o segredo de repositório `SONAR_TOKEN` e as variáveis `SONAR_PROJECT_KEY` e `SONAR_ORGANIZATION` no GitHub. Quando os três valores estiverem presentes, o job adicional executará o scanner e reportará o Quality Gate no pull request. O gate deve avaliar principalmente código novo; cobertura global não é usada como meta artificial.

Para validar a integração manualmente, suba o Compose, abra o Swagger e crie um pedido. A API persiste o pedido como `pendente` e registra o evento `OrderCreated` na outbox na mesma transação. Um publicador em background entrega o evento à exchange `order.events`, direcionado à fila `order.created.v1`. O Worker o move para `processando`, aguarda cinco segundos e o finaliza. Cada transição fica registrada em `order_status_history` e é publicada na exchange `order.status.events`.

Pela interface, crie um pedido em `http://localhost:3000/orders` e abra seus detalhes. A tela recebe as transições por SSE e atualiza o status e o histórico sem recarregar. Se a conexão SSE ficar indisponível, listagem, detalhe e histórico fazem polling apenas enquanto o pedido estiver `pendente` ou `processando`. A tabela inicia pelos pedidos mais recentes e permite ordenar as colunas; idioma e tema são preferências persistidas localmente.

### Mensageria

O contrato compartilhado `OrderCreated` contém `messageId`, `orderId`, `correlationId`, `eventType` e `occurredAt`. Para cada pedido criado, `correlationId` é igual a `orderId` e `eventType` é `OrderCreated`.

Após cada mudança persistida pelo Worker, o contrato `OrderStatusChanged` é publicado com `messageId`, `orderId`, `correlationId`, `eventType`, `status` e `occurredAt`. O status usa o mesmo valor canônico da API (`pendente`, `processando` ou `finalizado`); `correlationId` continua igual a `orderId`. A API consome a fila `order.status.changed.v1` e retransmite os eventos em `GET /orders/events` usando Server-Sent Events (evento `order.status.changed`).

O publicador da outbox declara explicitamente exchange direta, fila e binding, publica mensagens persistentes e aguarda a confirmação do broker antes de marcar o evento como entregue. O Worker usa `ack` manual, prefetch de uma mensagem e as transições do agregado para tratar reentregas com idempotência: `Pendente` inicia o processamento, `Processando` conclui após cinco segundos e `Finalizado` é somente reconhecido.

Falhas transitórias são republicadas na fila de retry, que possui TTL de cinco segundos e devolve a mensagem à fila principal. Após `RABBITMQ_MAX_DELIVERY_ATTEMPTS`, ou quando a mensagem é inválida, ela é encaminhada para a DLQ `order.created.v1.error`. Os nomes de fila são versionados porque os argumentos de uma fila RabbitMQ são imutáveis depois da criação; uma alteração de topologia em produção deve criar uma nova versão e drenar a anterior.

O **Transactional Outbox Pattern** elimina a janela entre gravar o pedido ou sua transição de status e publicar os eventos `OrderCreated` e `OrderStatusChanged`: estado e evento são confirmados em uma única transação PostgreSQL. A tabela `outbox_messages` mantém payload, tentativas, último erro, próxima tentativa e data de publicação. O publicador obtém uma mensagem pendente com bloqueio `FOR UPDATE SKIP LOCKED`, aguarda a confirmação do RabbitMQ e só então marca a mensagem como publicada. A entrega permanece *at-least-once*: uma interrupção após a confirmação do broker pode causar reentrega, tratada com segurança pelo consumidor idempotente.

O realtime é intencionalmente orientado à demonstração com uma instância de API: o navegador sempre pode recuperar o estado pelo endpoint de histórico. Em uma implantação horizontal, cada instância precisaria da sua própria fila de fan-out ou de um backplane (por exemplo, Redis/SignalR).

### Observabilidade

A API e o Worker expõem health checks de readiness e liveness e incluem tracing OpenTelemetry para requests HTTP, chamadas HTTP de saída e publicação RabbitMQ. Para demonstrar os spans localmente sem acoplar o projeto a um fornecedor, habilite o exportador de console em cada processo:

```powershell
$env:Observability__ConsoleExporterEnabled = "true"
```

O trace registra o serviço, o tipo de operação de mensageria, `order.id` e `messaging.message.id`. Em um deploy, o exportador de console deve ser substituído por um collector OTLP, sem alterar as regras de negócio ou o contrato de mensagens.

## Endpoints disponíveis

- `POST /orders` cria um pedido em `pendente`.
- `GET /orders` lista pedidos, do mais recente para o mais antigo.
- `GET /orders/{id}` retorna um pedido ou `404`.
- `GET /orders/{id}/history` retorna o histórico imutável de status ou `404`.
- `GET /orders/events` abre o stream SSE de `OrderStatusChanged`.
- `GET /health` verifica a API, o PostgreSQL e o RabbitMQ.
- `GET /health/live` verifica que o processo está vivo, sem depender do banco.

Em ambiente `Development`, a documentação interativa está disponível em `/swagger`.

Exemplo de criação:

```json
{
  "cliente": "Ana Silva",
  "produto": "Notebook",
  "valor": 4999.90
}
```

## Decisões técnicas

- **Monólito modular:** `Domain` não conhece HTTP, EF Core ou PostgreSQL; `Application` define casos de uso e portas; `Infrastructure` implementa persistência; `Api` fica só como adaptador HTTP. É mais simples de explicar e operar que microserviços, mas mantém fronteiras para API e worker evoluírem separadamente.
- **Transições no domínio:** o agregado `Order` permite apenas `Pendente → Processando → Finalizado`. Assim, qualquer futuro consumidor de mensagem reutiliza a mesma regra e não depende de uma validação exclusiva do controller.
- **PostgreSQL + EF Core:** o mapeamento usa precisão `numeric(18,2)` para dinheiro, evita erro de ponto flutuante e mantém a migration versionada junto ao código.
- **Contrato HTTP em português e `snake_case`:** `cliente`, `produto`, `valor`, `status` e `data_criacao` tornam a API explícita para o desafio sem expor a entidade do EF Core.
- **Health checks separados:** readiness (`/health`) inclui o banco; liveness (`/health/live`) não inclui dependências externas, evitando reinícios indevidos quando o PostgreSQL estiver temporariamente indisponível.
- **Segredos fora do repositório:** a string de conexão vem de `ConnectionStrings__OrdersDatabase`; `.env.example` só documenta o formato e `.env` continua ignorado pelo Git.
- **HTTPS por ambiente:** IIS Express mantém redirecionamento HTTPS. O Compose local o desabilita porque expõe apenas HTTP; no deploy, o proxy reverso será responsável por TLS e essa configuração continuará explícita.
- **RabbitMQ isolado por configuração:** host, credenciais, exchange, fila e routing key são variáveis de ambiente. A UI de management existe somente no Compose local para demonstrar a topologia e os consumidores, sem virar uma dependência da aplicação.
- **Worker independente:** o serviço executa em processo e imagem próprios, reutilizando somente contratos, aplicação e infraestrutura necessários. Ele expõe health checks internamente e consome a fila sem acoplar regras de negócio ao AMQP.
- **Histórico como trilha de auditoria:** cada mudança válida do agregado grava um registro append-only com instante, origem e identificador da mensagem quando aplicável. Isso explica o estado atual ao usuário e oferece base para auditoria, sem tornar o histórico uma nova fonte de verdade.
- **SSE antes de WebSocket:** o servidor só precisa notificar o navegador; SSE é unidirecional, nativo do browser e mais simples de operar. A API consome os eventos de status, atualiza os clientes conectados e o TanStack Query mantém o cache coerente. Polling condicional preserva a experiência quando a conexão não existe ou é interrompida.
- **Frontend orientado à operação:** React, TypeScript estrito, MUI, React Router e TanStack Query mantêm a interface pequena e tipada. O TanStack Query concentra cache, mutações, polling e estados de requisição; um cliente `fetch` enxuto trata somente o transporte HTTP.
- **Proxy de mesma origem no Compose:** o Nginx do frontend encaminha o prefixo `/api` internamente para a API. Isso evita colisão com as rotas da SPA (inclusive em um refresh), não expõe credenciais de infraestrutura ao navegador e dispensa uma regra de CORS ampla para a demonstração local.
- **Preferências de interface:** o frontend inicia em português (Brasil) e tema claro. Idioma e tema são persistidos no `localStorage`; valores continuam trafegando como número JSON em BRL e datas como ISO 8601, sendo formatados somente na apresentação conforme o idioma selecionado.
- **Ordenação no cliente:** a API retorna a lista em ordem de criação, mas a interface permite reordenar a coleção já carregada sem novas chamadas. A regra é pura e testada, mantendo a visualização responsiva simples sem antecipar paginação ou ordenação server-side.
- **Observabilidade por configuração:** OpenTelemetry instrumenta HTTP e operações de publicação RabbitMQ; o exportador de console só é ligado por configuração para manter o ambiente local demonstrável sem introduzir uma plataforma de telemetria prematuramente. Os relatórios de cobertura e o Quality Gate ficam no CI; o SonarQube Cloud é ativado apenas quando seus segredos forem configurados no GitHub.
