# Order Management Challenge

Sistema de gestão de pedidos desenvolvido como desafio técnico, com .NET, React, PostgreSQL, RabbitMQ e Docker.

## Estado atual

O incremento `feature/frontend-orders` adiciona a interface React para listar, criar e acompanhar pedidos. A API publica `OrderCreated` após persistir um pedido e o Worker o processa de forma assíncrona, com retentativas e fila de erro.

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

Os testes de domínio protegem a criação e as transições obrigatórias de status. O teste de aplicação protege o limite transacional do caso de uso: o pedido é persistido antes de `OrderCreated` ser publicado.

```powershell
dotnet test src/OrderManagement.sln --no-restore
```

Os testes de frontend cobrem a submissão e a validação do formulário, além da apresentação dos status:

```powershell
cd frontend
npm ci
npm run test
npm run build
```

Para validar a integração manualmente, suba o Compose, abra o Swagger e crie um pedido. A API o persiste como `pendente` e publica um evento na exchange `order.events`, direcionado à fila `order.created.v1`. O Worker o move para `processando` e o finaliza cinco segundos depois.

Pela interface, crie um pedido em `http://localhost:3000/orders`. A listagem e o detalhe fazem polling apenas enquanto o pedido estiver `pendente` ou `processando`; isso mantém a tela atualizada sem antecipar a implementação de SSE. A tabela inicia pelos pedidos mais recentes e permite ordenar as colunas; idioma e tema são preferências persistidas localmente.

### Mensageria

O contrato compartilhado `OrderCreated` contém `messageId`, `orderId`, `correlationId`, `eventType` e `occurredAt`. Para cada pedido criado, `correlationId` é igual a `orderId` e `eventType` é `OrderCreated`.

A API declara explicitamente exchange direta, fila e binding, publica mensagens persistentes e aguarda a confirmação do broker antes de retornar sucesso. O Worker usa `ack` manual, prefetch de uma mensagem e as transições do agregado para tratar reentregas com idempotência: `Pendente` inicia o processamento, `Processando` conclui após cinco segundos e `Finalizado` é somente reconhecido.

Falhas transitórias são republicadas na fila de retry, que possui TTL de cinco segundos e devolve a mensagem à fila principal. Após `RABBITMQ_MAX_DELIVERY_ATTEMPTS`, ou quando a mensagem é inválida, ela é encaminhada para a DLQ `order.created.v1.error`. Os nomes de fila são versionados porque os argumentos de uma fila RabbitMQ são imutáveis depois da criação; uma alteração de topologia em produção deve criar uma nova versão e drenar a anterior. Nesta etapa, persistência no PostgreSQL e publicação no RabbitMQ ainda não formam uma única transação: se o banco confirmar e o broker falhar, o pedido fica persistido sem evento. Este trade-off é deliberado e será resolvido pelo **Outbox Pattern** no refinamento de confiabilidade.

## Endpoints disponíveis

- `POST /orders` cria um pedido em `pendente`.
- `GET /orders` lista pedidos, do mais recente para o mais antigo.
- `GET /orders/{id}` retorna um pedido ou `404`.
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
- **Frontend orientado à operação:** React, TypeScript estrito, MUI, React Router e TanStack Query mantêm a interface pequena e tipada. O TanStack Query concentra cache, mutações, polling e estados de requisição; um cliente `fetch` enxuto trata somente o transporte HTTP.
- **Proxy de mesma origem no Compose:** o Nginx do frontend encaminha o prefixo `/api` internamente para a API. Isso evita colisão com as rotas da SPA (inclusive em um refresh), não expõe credenciais de infraestrutura ao navegador e dispensa uma regra de CORS ampla para a demonstração local.
- **Preferências de interface:** o frontend inicia em português (Brasil) e tema claro. Idioma e tema são persistidos no `localStorage`; valores continuam trafegando como número JSON em BRL e datas como ISO 8601, sendo formatados somente na apresentação conforme o idioma selecionado.
- **Ordenação no cliente:** a API retorna a lista em ordem de criação, mas a interface permite reordenar a coleção já carregada sem novas chamadas. A regra é pura e testada, mantendo a visualização responsiva simples sem antecipar paginação ou ordenação server-side.
