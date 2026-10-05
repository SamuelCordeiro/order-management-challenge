# Order Management Challenge

Sistema de gestão de pedidos desenvolvido como desafio técnico, com .NET, React, PostgreSQL, RabbitMQ e Docker.

## Estado atual

O incremento `feature/backend-orders` implementa o núcleo HTTP, persistência e execução local com Docker Compose. Mensageria, worker e frontend permanecem nos próximos incrementos para manter cada mudança pequena e demonstrável.

## Executar a API

### IIS Express ou dotnet run

1. Configure uma conexão PostgreSQL local com User Secrets. Os valores ficam fora do repositório:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:OrdersDatabase" "Host=localhost;Port=5432;Database=orders;Username=postgres;Password=sua-senha" --project src/OrderManagement.Api
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
2. Suba a API e o PostgreSQL:

   ```powershell
   docker compose up --build
   ```

O Compose aguarda o PostgreSQL estar saudável, a API aplica a migration automaticamente e o Swagger fica em `http://localhost:8080/swagger`. Para encerrar mantendo os dados, execute `docker compose down`.

### Testes

Os testes de domínio protegem a criação do pedido e as transições obrigatórias de status:

```powershell
dotnet test src/OrderManagement.sln --no-restore
```

Para validar a integração manualmente, suba o Compose, abra o Swagger, crie um pedido e consulte `GET /orders`. O pedido deve permanecer como `pendente` até a entrega do Worker no próximo incremento.

## Endpoints disponíveis

- `POST /orders` cria um pedido em `pendente`.
- `GET /orders` lista pedidos, do mais recente para o mais antigo.
- `GET /orders/{id}` retorna um pedido ou `404`.
- `GET /health` verifica a API e o PostgreSQL.
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
