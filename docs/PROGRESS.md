# Registro de Progresso do Projeto

Este arquivo registra o progresso de cada tarefa do plano de implementação (`/docs/04-plano-de-implementacao.md`). Deve ser atualizado pelo agente ao concluir cada tarefa. Não editar manualmente sem necessidade.

---

## Tarefa 01 — Criar a estrutura base do repositório
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `.editorconfig` — convenções de formatação
  - `apps/api-dotnet/.env.example` — variáveis de ambiente do back-end
  - `apps/api-dotnet/README.md` — documentação do back-end
  - `apps/web-next/.env.example` — variáveis de ambiente do front-end
  - `apps/web-next/README.md` — documentação do front-end
  - `samples/README.md` — documentação da pasta de fixtures demo
  - `samples/.gitkeep` — preservar diretório no git
  - `scripts/README.md` — documentação da pasta de scripts
  - `scripts/.gitkeep` — preservar diretório no git
- **Testes criados/executados:** validação manual da estrutura de diretórios
- **Validação manual:** estrutura verificada via `find` — todas as pastas (`apps/api-dotnet`, `apps/web-next`, `samples`, `scripts`, `docs`) presentes; `.editorconfig`, `.gitignore`, `.env.example` (front e back) criados; READMEs por app explicam propósito e stack
- **Riscos/pendências:** nenhum
- **Data de conclusão:** 2026-03-28

---

## Tarefa 02 — Inicializar a aplicação back-end .NET
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/api-dotnet/WhatsAppNewsPortal.slnx` — solution file (.NET 10)
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/WhatsAppNewsPortal.Api.csproj` — projeto Web API
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Program.cs` — entry point com health check, CORS, logging e endpoint de teste
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/appsettings.json` — configuração base
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/appsettings.Development.json` — configuração de dev
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/WhatsAppNewsPortal.Api.Tests.csproj` — projeto de testes xUnit
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/HealthAndPingTests.cs` — testes de integração
- **Testes criados/executados:**
  - `HealthAndPingTests.Health_ReturnsOkWithStatus` — verifica GET /health retorna 200 com `{"status":"healthy"}`
  - `HealthAndPingTests.Ping_ReturnsOkWithPong` — verifica GET /api/ping retorna 200 com `{"message":"pong"}`
  - Resultado: 2 aprovados, 0 falhas
- **Validação manual:** `dotnet build` compila sem warnings/erros; `dotnet test` passa 2/2 testes; CORS configurado via variável `CORS_ORIGIN` (default `http://localhost:3000`); logging estruturado via console
- **Riscos/pendências:** nenhum
- **Data de conclusão:** 2026-03-28

---

## Tarefa 03 — Definir a estrutura interna do back-end
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `Common/PipelineStatus.cs` — enum dos estados do pipeline (Discovered, Processing, Draft, Published, Failed)
  - `Common/SourceType.cs` — enum dos tipos de fonte (Official, BetaSpecialized)
  - `Common/EditorialType.cs` — enum dos tipos editoriais (OfficialNews, BetaNews)
  - `Sources/Domain/Source.cs` — entidade de fonte monitorada
  - `Sources/Domain/SourceItem.cs` — entidade de item descoberto
  - `Sources/Application/ISourceRepository.cs` — contrato de acesso a fontes
  - `Sources/Application/ISourceItemRepository.cs` — contrato de acesso a itens
  - `Ingestion/Application/IIngestionAdapter.cs` — contrato do adapter de ingestão
  - `ContentProcessing/Application/IContentProcessor.cs` — contrato de normalização
  - `AiGeneration/Application/IAiClassifier.cs` — contrato de classificação IA + DTO ArticleMetadata
  - `AiGeneration/Application/IAiArticleGenerator.cs` — contrato de geração de artigo IA
  - `Articles/Domain/Article.cs` — entidade de artigo
  - `Articles/Application/IArticleRepository.cs` — contrato de acesso a artigos
  - `Demo/Application/IDemoPipelineService.cs` — contrato do pipeline demo
- **Testes criados/executados:** build compila 0 warnings/0 erros; 2 testes existentes continuam passando
- **Validação manual:** estrutura de módulos verificada — 7 módulos (Common, Sources, Ingestion, ContentProcessing, AiGeneration, Articles, Demo) com separação Domain/Application; adapters externos isolados via interfaces; entidades e DTOs em namespaces distintos
- **Riscos/pendências:** nenhum; pastas Infrastructure serão populadas nas tarefas seguintes quando as implementações concretas forem criadas
- **Data de conclusão:** 2026-03-28

---

## Tarefa 04 — Configurar PostgreSQL e EF Core
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `WhatsAppNewsPortal.Api/WhatsAppNewsPortal.Api.csproj` — adicionados pacotes Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1, Microsoft.EntityFrameworkCore 10.0.5, Microsoft.EntityFrameworkCore.Relational 10.0.5, Microsoft.EntityFrameworkCore.Design 10.0.5
  - `WhatsAppNewsPortal.Api/Infrastructure/Data/AppDbContext.cs` — DbContext com mapeamento de Source, SourceItem e Article (tabelas snake_case, enums como string, índices, FKs, text[] para tags)
  - `WhatsAppNewsPortal.Api/Infrastructure/Data/Migrations/20260328190212_InitialCreate.cs` — migration inicial com 3 tabelas (sources, source_items, articles)
  - `WhatsAppNewsPortal.Api/Infrastructure/Data/Migrations/20260328190212_InitialCreate.Designer.cs` — designer da migration
  - `WhatsAppNewsPortal.Api/Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs` — snapshot do modelo
  - `WhatsAppNewsPortal.Api/Program.cs` — registro do AppDbContext via DI com connection string de ConnectionStrings__DefaultConnection ou DATABASE_URL
  - `WhatsAppNewsPortal.Api/appsettings.json` — adicionada seção ConnectionStrings (vazia para produção)
  - `WhatsAppNewsPortal.Api/appsettings.Development.json` — connection string local para dev (localhost:5432/whatsapp_news)
  - `WhatsAppNewsPortal.Api.Tests/HealthAndPingTests.cs` — atualizado para substituir DbContext por InMemory nos testes
  - `WhatsAppNewsPortal.Api.Tests/DbContextTests.cs` — 3 novos testes de integração (persistência de Source, SourceItem e Article)
  - `WhatsAppNewsPortal.Api.Tests/WhatsAppNewsPortal.Api.Tests.csproj` — adicionado pacote Microsoft.EntityFrameworkCore.InMemory 10.0.5
- **Testes criados/executados:**
  - `DbContextTests.CanPersistAndReadSource` — persiste e lê Source com tipo Official
  - `DbContextTests.CanPersistSourceItemLinkedToSource` — persiste SourceItem vinculado a Source com FK
  - `DbContextTests.CanPersistArticleLinkedToSourceItem` — persiste Article com tags, slug, editorial type e FK para SourceItem
  - `HealthAndPingTests.Health_ReturnsOkWithStatus` — existente, agora com InMemory DB
  - `HealthAndPingTests.Ping_ReturnsOkWithPong` — existente, agora com InMemory DB
  - Resultado: 5 aprovados, 0 falhas
- **Validação manual:** `dotnet build` compila com 0 warnings/0 erros; `dotnet test` passa 5/5; migration gerada com tabelas sources, source_items e articles; connection string configurável por ambiente via ConnectionStrings:DefaultConnection ou DATABASE_URL; `dotnet ef` tools instalados globalmente (v10.0.5); para aplicar migration em banco local usar `dotnet ef database update --project WhatsAppNewsPortal.Api`
- **Riscos/pendências:** nenhum; para aplicar as migrations é necessário um PostgreSQL rodando (local ou remoto); a tarefa de modelar entidades completas (Tarefa 05) adicionará campos faltantes como ArticleSourceReference e ProcessingLog
- **Data de conclusão:** 2026-03-28

---

## Tarefa 05 — Modelar entidades principais do domínio
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `Sources/Domain/Source.cs` — campos atualizados: `Url`→`BaseUrl`, `Active`→`IsActive`, adicionados `FeedUrl` e `UpdatedAt`
  - `Sources/Domain/SourceItem.cs` — adicionados: `CanonicalUrl`, `PublishedAt`, `ContentHash`, `SourceClassification`, `IsDemoItem`, `CreatedAt`, `UpdatedAt`; `ProcessedAt` renomeado para `PublishedAt`
  - `Articles/Domain/Article.cs` — renomeados: `Summary`→`Excerpt`, `Content`→`ContentHtml`, `EditorialType`→`ArticleType`; adicionados: `MetaTitle`, `SchemaJsonLd`, `Category`, `UpdatedAt`
  - `Articles/Domain/ArticleSourceReference.cs` — nova entidade: id, articleId, sourceName, sourceUrl, referenceType
  - `Common/ProcessingLog.cs` — nova entidade: id, sourceItemId (opcional), stepName, status, message, createdAt
  - `Infrastructure/Data/AppDbContext.cs` — DbSets e configurações para todas as 5 entidades; índices em CanonicalUrl, ContentHash, Category, SourceItemId, CreatedAt
  - `Infrastructure/Data/Migrations/20260328191330_AddDomainEntities.cs` — migration incremental sobre InitialCreate
  - `WhatsAppNewsPortal.Api.Tests/DbContextTests.cs` — testes atualizados com nomes novos + 2 novos testes (ArticleSourceReference, ProcessingLog com e sem SourceItemId)
- **Testes criados/executados:**
  - `DbContextTests.CanPersistAndReadSource` — verifica BaseUrl, FeedUrl, IsActive
  - `DbContextTests.CanPersistSourceItemLinkedToSource` — verifica ContentHash, SourceClassification, IsDemoItem
  - `DbContextTests.CanPersistArticleLinkedToSourceItem` — verifica Excerpt, ContentHtml, MetaTitle, Category, ArticleType
  - `DbContextTests.CanPersistArticleSourceReference` — nova; valida persistência com FK para Article
  - `DbContextTests.CanPersistProcessingLog` — nova; valida log com SourceItemId
  - `DbContextTests.CanPersistProcessingLogWithoutSourceItem` — nova; valida log sem SourceItemId (nullable)
  - `HealthAndPingTests.Health_ReturnsOkWithStatus` — existente, continua passando
  - `HealthAndPingTests.Ping_ReturnsOkWithPong` — existente, continua passando
  - Resultado: **8 aprovados, 0 falhas**
- **Validação manual:** `dotnet build` compila com 0 warnings/0 erros; `dotnet test` passa 8/8; migration `AddDomainEntities` gerada corretamente — altera 3 tabelas existentes (sources, source_items, articles) e cria 2 novas (article_source_references, processing_logs); schema coerente com o modelo editorial do projeto
- **Riscos/pendências:** para aplicar as migrations em banco real executar `dotnet ef database update --project WhatsAppNewsPortal.Api`; o aviso "may result in loss of data" na geração da migration é esperado pois há renomeações de colunas — sem impacto pois o banco de dev ainda não tem dados relevantes
- **Data de conclusão:** 2026-03-28

---

## Tarefa 06 — Criar seed das fontes oficiais e da fonte beta especializada
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `Sources/Infrastructure/SourceSeeder.cs` — seeder com as 4 fontes; idempotente por verificação de `BaseUrl` antes de inserir
  - `Infrastructure/Data/AppDbContext.cs` — adicionado `HasIndex(e => e.BaseUrl).IsUnique()` na configuração de `Source`
  - `Infrastructure/Data/Migrations/20260328203410_AddSourceBaseUrlUniqueIndex.cs` — migration que adiciona índice único em `sources.BaseUrl`
  - `Infrastructure/Data/Migrations/20260328203410_AddSourceBaseUrlUniqueIndex.Designer.cs` — designer da migration
  - `Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs` — snapshot atualizado
  - `Program.cs` — chamada a `SourceSeeder.SeedAsync(db)` após migrations automáticas (quando `RUN_MIGRATIONS_ON_STARTUP=true`)
  - `WhatsAppNewsPortal.Api.Tests/SourceSeederTests.cs` — 6 testes de integração do seeder
- **Fontes seedadas:**
  - `WhatsApp Blog` — `Official` — https://blog.whatsapp.com — feed: https://blog.whatsapp.com/rss
  - `WhatsApp Business Blog` — `Official` — https://business.whatsapp.com/blog — sem feed configurado
  - `WhatsApp API Documentation` — `Official` — https://developers.facebook.com/docs/whatsapp — sem feed
  - `WABetaInfo` — `BetaSpecialized` — https://wabetainfo.com — feed: https://wabetainfo.com/feed/
- **Testes criados/executados:**
  - `SourceSeederTests.Seed_Creates_Four_Sources` — seed cria 4 fontes
  - `SourceSeederTests.Seed_Is_Idempotent` — re-execução 3x não cria duplicatas
  - `SourceSeederTests.Seed_WABetaInfo_Is_BetaSpecialized` — WABetaInfo classificada como `BetaSpecialized`
  - `SourceSeederTests.Seed_Official_Sources_Are_Official` — 3 fontes com tipo `Official`
  - `SourceSeederTests.Seed_WhatsAppBlog_Has_FeedUrl` — WhatsApp Blog tem FeedUrl definida
  - `SourceSeederTests.Seed_All_Sources_Are_Active` — todas as fontes estão ativas
  - Resultado: **14 aprovados, 0 falhas** (6 novos + 8 anteriores)
- **Validação manual:** `dotnet build` compila 0 warnings/0 erros; `dotnet test` passa 14/14; migration `AddSourceBaseUrlUniqueIndex` gerada corretamente adicionando índice único em `sources.BaseUrl`; para aplicar em banco real: `dotnet ef database update --project WhatsAppNewsPortal.Api`; seed é acionado automaticamente ao iniciar a API com `RUN_MIGRATIONS_ON_STARTUP=true`
- **Riscos/pendências:** os FeedUrls das fontes (blog.whatsapp.com/rss e wabetainfo.com/feed/) serão validados na Tarefa 08 (adapter RSS); URLs podem precisar de ajuste se o feed real estiver em outro caminho
- **Data de conclusão:** 2026-03-28

---

## Tarefa 07 — Definir contratos internos do pipeline
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 08 — Implementar adapter de ingestão por RSS
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 09 — Implementar adapter de ingestão por HTML simples
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 10 — Implementar normalização de SourceItem
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 11 — Implementar deduplicação básica
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 12 — Implementar cliente Gemini com abstração própria
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 13 — Implementar etapa de classificação e metadata com Gemini Flash-Lite
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 14 — Implementar etapa de geração de artigo final com Gemini Flash
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 15 — Persistir draft e referências de origem
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 16 — Implementar publicação manual do draft
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 17 — Implementar orquestrador manual do pipeline real
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 18 — Implementar modo demo RunDemoPipeline
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 19 — Expor endpoints mínimos para o front-end
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 20 — Inicializar o front-end Next.js
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 21 — Implementar página inicial do portal
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 22 — Implementar página de artigo
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 23 — Implementar página de categoria
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 24 — Implementar SEO técnico do front
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 25 — Implementar botão ou rota operacional de demo/admin mínima
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 26 — Implementar logs e observabilidade mínima
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 27 — Implementar tratamento robusto de falhas do pipeline
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 28 — Popular o sistema com artigos reais
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 29 — Deploy do banco no Render Postgres
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 30 — Deploy do back-end .NET no Render
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 31 — Deploy do front-end Next.js na Vercel
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 32 — Validar fluxo real ponta a ponta em produção
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 33 — Validar modo demo em produção
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**

---

## Tarefa 34 — Revisão final de qualidade editorial, UX e confiabilidade
- **Status:** pendente
- **Arquivos criados/alterados:**
- **Testes criados/executados:**
- **Validação manual:**
- **Riscos/pendências:**
- **Data de conclusão:**
