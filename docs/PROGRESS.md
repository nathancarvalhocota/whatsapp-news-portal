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
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `Ingestion/Application/DiscoveredItemDto.cs` — DTO de item descoberto pela ingestão (antes da persistência)
  - `ContentProcessing/Application/NormalizedItemDto.cs` — DTO de item após normalização, entrada para classificação
  - `AiGeneration/Application/ClassificationResultDto.cs` — resultado da classificação Gemini Flash-Lite (IsRelevant, EditorialType, Slug, MetaTitle, MetaDescription, Excerpt, Tags, EditorialNote)
  - `AiGeneration/Application/GeneratedArticleDto.cs` — resultado da geração de artigo Gemini Flash (Title, Excerpt, ContentHtml, BetaDisclaimer)
  - `Articles/Application/PublishArticleResultDto.cs` — resultado da publicação de um draft
  - `Articles/Application/IArticlePublisher.cs` — interface do serviço de publicação
  - `Demo/Application/DemoPipelineResultDto.cs` — resultado da execução do pipeline demo (contadores + erros)
  - `Ingestion/Application/IIngestionAdapter.cs` — atualizado: retorna `List<DiscoveredItemDto>` em vez de `List<SourceItem>`
  - `ContentProcessing/Application/IContentProcessor.cs` — atualizado: retorna `NormalizedItemDto`
  - `AiGeneration/Application/IAiClassifier.cs` — atualizado: aceita `NormalizedItemDto`, retorna `ClassificationResultDto`; `ArticleMetadata` removido
  - `AiGeneration/Application/IAiArticleGenerator.cs` — atualizado: aceita `NormalizedItemDto` + `ClassificationResultDto`, retorna `GeneratedArticleDto`
  - `Demo/Application/IDemoPipelineService.cs` — atualizado: retorna `Task<DemoPipelineResultDto>`
  - `WhatsAppNewsPortal.Api.Tests/PipelineContractTests.cs` — 13 novos testes unitários
- **Testes criados/executados:**
  - `PipelineContractTests.DiscoveredItemDto_Defaults_AreValid`
  - `PipelineContractTests.DiscoveredItemDto_DemoFlag_SetCorrectly`
  - `PipelineContractTests.NormalizedItemDto_JsonRoundTrip_PreservesAllFields`
  - `PipelineContractTests.ClassificationResultDto_BetaSource_MustHaveBetaNewsType`
  - `PipelineContractTests.ClassificationResultDto_IrrelevantItem_HasDiscardReason`
  - `PipelineContractTests.ClassificationResultDto_JsonRoundTrip_PreservesAllFields`
  - `PipelineContractTests.GeneratedArticleDto_BetaItem_HasBetaDisclaimer`
  - `PipelineContractTests.GeneratedArticleDto_OfficialItem_HasNoBetaDisclaimer`
  - `PipelineContractTests.GeneratedArticleDto_JsonRoundTrip_PreservesAllFields`
  - `PipelineContractTests.PublishArticleResultDto_JsonRoundTrip_PreservesAllFields`
  - `PipelineContractTests.DemoPipelineResultDto_Defaults_AreValid`
  - `PipelineContractTests.DemoPipelineResultDto_HasErrors_TrueWhenErrorsPresent`
  - `PipelineContractTests.DemoPipelineResultDto_JsonRoundTrip_PreservesAllFields`
  - Resultado: **27 aprovados, 0 falhas** (13 novos + 14 anteriores)
- **Validação manual:** `dotnet build` compila com 0 warnings/0 erros; `dotnet test` passa 27/27; todas as interfaces do pipeline agora usam DTOs dedicados em vez de entidades de domínio ou tipos anônimos; fluxo do pipeline: `IIngestionAdapter` → `DiscoveredItemDto` → [persistência] → `IContentProcessor` → `NormalizedItemDto` → `IAiClassifier` → `ClassificationResultDto` → `IAiArticleGenerator` → `GeneratedArticleDto` → [draft] → `IArticlePublisher` → `PublishArticleResultDto`
- **Riscos/pendências:** nenhum; as implementações concretas das interfaces serão criadas nas tarefas 08–18
- **Data de conclusão:** 2026-03-28

---

## Tarefa 08 — Implementar adapter de ingestão por RSS
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `WhatsAppNewsPortal.Api/Properties/AssemblyInfo.cs` — `InternalsVisibleTo` para o projeto de testes
  - `WhatsAppNewsPortal.Api/Ingestion/Infrastructure/RssIngestionAdapter.cs` — implementação do adapter RSS/Atom; parsing com `XDocument`; normalização de URL; stripping do prefixo `ddd,` do RFC 2822; retorna lista vazia em falha HTTP ou XML inválido
  - `WhatsAppNewsPortal.Api/Program.cs` — registro de `HttpClient` typed para `RssIngestionAdapter` (timeout 30s, User-Agent) + DI `IIngestionAdapter → RssIngestionAdapter`
  - `WhatsAppNewsPortal.Api.Tests/RssIngestionAdapterTests.cs` — 16 testes unitários
  - `samples/fixtures/rss-feed-sample.xml` — fixture RSS 2.0 (WhatsApp Blog)
  - `samples/fixtures/atom-feed-sample.xml` — fixture Atom (WABetaInfo)
- **Testes criados/executados:**
  - `ParseFeed_Rss2_ExtractsCorrectNumberOfItems` — feed RSS 2.0 com 2 itens retorna 2
  - `ParseFeed_Rss2_ExtractsTitleLinkAndDate` — título, URL e data extraídos corretamente
  - `ParseFeed_Rss2_ExtractsDescription` — conteúdo bruto (description) extraído
  - `ParseFeed_Rss2_PublishedAtIsUtc` — data sempre em UTC
  - `ParseFeed_Atom_ExtractsCorrectNumberOfItems` — feed Atom com 2 entradas retorna 2
  - `ParseFeed_Atom_ExtractsTitleLinkAndDate` — título, URL e data do Atom extraídos
  - `ParseFeed_Atom_ExtractsSummary` — summary do Atom extraído
  - `ParseFeed_Rss2_DeduplicatesDuplicateUrlsWithinBatch` — 3 itens com 2 URLs únicas retorna 2
  - `NormalizeUrl_LowercasesSchemeAndHost` — esquema e host em minúsculo
  - `NormalizeUrl_TrimsWhitespace` — whitespace removido
  - `NormalizeUrl_ReturnsLowercasedInputForInvalidUri` — URI inválida retorna lowercased
  - `FetchItemsAsync_ReturnsEmptyOnHttpRequestException` — falha de rede retorna lista vazia
  - `FetchItemsAsync_ReturnsEmptyOnNonSuccessStatusCode` — HTTP 500 retorna lista vazia
  - `FetchItemsAsync_ReturnsEmptyOnInvalidXml` — XML inválido retorna lista vazia
  - `FetchItemsAsync_ReturnsEmptyWhenSourceHasNoFeedUrl` — source sem FeedUrl retorna vazio
  - `FetchItemsAsync_ReturnsEmptyWhenSourceHasEmptyFeedUrl` — FeedUrl em branco retorna vazio
  - `ParseFeed_Rss2_IsDemoItemDefaultsFalse` — IsDemoItem padrão false
  - Resultado: **44 aprovados, 0 falhas** (17 novos + 27 anteriores)
- **Validação manual:** `dotnet build` compila com 0 warnings/0 erros; `dotnet test` passa 44/44; adapter registrado no DI com `AddHttpClient<RssIngestionAdapter>`; fontes com FeedUrl (WhatsApp Blog e WABetaInfo) podem ser testadas contra os feeds reais chamando `FetchItemsAsync` com a `Source` correta; falhas de parsing/HTTP são silenciosas e retornam lista vazia sem derrubar o processo
- **Riscos/pendências:** os FeedUrls reais (`https://blog.whatsapp.com/rss` e `https://wabetainfo.com/feed/`) ainda não foram validados contra os feeds ao vivo — serão testados na Tarefa 17 (orquestrador real); deduplicação contra o banco (por `canonicalUrl`) é responsabilidade da Tarefa 11
- **Data de conclusão:** 2026-03-28

---

## Tarefa 09 — Implementar adapter de ingestão por HTML simples
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `WhatsAppNewsPortal.Api/Ingestion/Application/IHtmlFetcher.cs` — interface para fetch de HTML com tratamento de falhas (retorna null)
  - `WhatsAppNewsPortal.Api/Ingestion/Application/HtmlSourceParserConfig.cs` — configuração de seletores CSS por fonte (ArticleLinkSelector, ArticleLinkPattern, TitleSelector, ContentSelector, DateSelector)
  - `WhatsAppNewsPortal.Api/Ingestion/Infrastructure/HtmlFetcher.cs` — implementação de IHtmlFetcher com HttpClient, timeout 30s e user-agent definido
  - `WhatsAppNewsPortal.Api/Ingestion/Infrastructure/HtmlIngestionAdapter.cs` — adapter IIngestionAdapter para fontes sem RSS; parsing com AngleSharp e seletores CSS configuráveis; configs pré-definidas para business.whatsapp.com e developers.facebook.com; métodos ExtractContent e ExtractTitle para extração de conteúdo bruto
  - `WhatsAppNewsPortal.Api/WhatsAppNewsPortal.Api.csproj` — adicionado pacote AngleSharp 1.4.0 (justificado: parser por seletores CSS configuráveis conforme requisito da tarefa)
  - `WhatsAppNewsPortal.Api/Program.cs` — registro DI de IHtmlFetcher→HtmlFetcher (HttpClient typed, timeout 30s, User-Agent) e HtmlIngestionAdapter
  - `samples/fixtures/whatsapp-business-blog-listing.html` — fixture HTML de página de listagem do WhatsApp Business Blog
  - `samples/fixtures/whatsapp-business-blog-article.html` — fixture HTML de página de artigo individual
  - `WhatsAppNewsPortal.Api.Tests/HtmlIngestionAdapterTests.cs` — 27 testes unitários
- **Testes criados/executados:**
  - `FetchItemsAsync_ReturnsEmptyWhenSourceHasFeedUrl` — fonte com FeedUrl é ignorada (tratada pelo RSS adapter)
  - `FetchItemsAsync_ReturnsEmptyOnHttpFailure` — falha de rede retorna lista vazia
  - `FetchItemsAsync_ReturnsEmptyOnNonSuccessStatusCode` — HTTP 500 retorna lista vazia
  - `FetchItemsAsync_ReturnsEmptyOnInvalidHtml` — HTML vazio retorna lista vazia
  - `FetchItemsAsync_ExtractsArticleLinksFromBusinessBlog` — extrai 3 links de artigos da fixture de listagem
  - `FetchItemsAsync_ExtractsTitlesFromListingPage` — extrai títulos corretamente dos links
  - `FetchItemsAsync_ResolvesRelativeUrls` — URLs relativas resolvidas para absolutas
  - `FetchItemsAsync_FiltersOutNonBlogLinks` — links de navegação/footer filtrados pelo padrão regex
  - `FetchItemsAsync_SetsSourceIdOnAllItems` — SourceId preenchido em todos os itens
  - `FetchItemsAsync_IsDemoItemDefaultsFalse` — IsDemoItem padrão false
  - `ParseListingPage_DeduplicatesDuplicateLinks` — URLs duplicadas deduplicadas no batch
  - `ParseListingPage_DefaultConfig_ExtractsAllAnchorLinks` — config default extrai todos os links válidos
  - `ResolveUrl_AbsoluteUrl_ReturnedAsIs` — URL absoluta mantida
  - `ResolveUrl_RelativeUrl_ResolvedAgainstBase` — URL relativa resolvida contra base
  - `ResolveUrl_RelativeUrl_WithoutBase_ReturnsNull` — URL relativa sem base retorna null
  - `ExtractContent_ExtractsArticleText` — extrai conteúdo de `<article>` via seletor
  - `ExtractContent_FallsBackToBody_WhenNoMatchingSelector` — fallback para body text
  - `ExtractContent_SkipsEmptyElements` — pula elementos vazios e tenta próximo seletor
  - `ExtractTitle_ExtractsH1` — extrai título de `<h1>`
  - `ExtractTitle_FallsBackToPageTitle` — fallback para `<title>` da página
  - `ExtractTitle_MultipleSelectors_TriesInOrder` — múltiplos seletores testados em ordem
  - `GetConfigForSource_ReturnsConfigForBusinessBlog` — config específica para business.whatsapp.com
  - `GetConfigForSource_ReturnsConfigForDevDocs` — config específica para developers.facebook.com
  - `GetConfigForSource_ReturnsDefaultForUnknownSource` — config default para fontes desconhecidas
  - `HtmlFetcher_ReturnsHtmlOnSuccess` — retorna HTML em sucesso
  - `HtmlFetcher_ReturnsNullOnNetworkFailure` — retorna null em falha de rede
  - `HtmlFetcher_ReturnsNullOnNonSuccessStatus` — retorna null em HTTP 404
  - Resultado: **71 aprovados, 0 falhas** (27 novos + 44 anteriores)
- **Validação manual:** `dotnet build` compila com 0 warnings/0 erros; `dotnet test` passa 71/71; adapter registrado no DI com `AddHttpClient<IHtmlFetcher, HtmlFetcher>` (timeout 30s, User-Agent); fontes sem FeedUrl (WhatsApp Business Blog e API Documentation) usarão o HtmlIngestionAdapter; fontes com FeedUrl continuam usando o RssIngestionAdapter; falhas de parsing/HTTP retornam lista vazia sem derrubar o processo; métodos `ExtractContent` e `ExtractTitle` disponíveis para uso na etapa de normalização (Tarefa 10)
- **Riscos/pendências:** os seletores CSS pré-configurados para business.whatsapp.com e developers.facebook.com são estimativas baseadas em estrutura HTML típica — podem precisar de ajuste após validação contra as páginas reais (Tarefa 17); AngleSharp 1.4.0 adicionado como dependência (única lib nova, justificada pelo requisito de parser com seletores CSS); a seleção entre RssIngestionAdapter e HtmlIngestionAdapter por fonte será orquestrada na Tarefa 17
- **Data de conclusão:** 2026-03-28

---

## Tarefa 10 — Implementar normalização de SourceItem
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `WhatsAppNewsPortal.Api/ContentProcessing/Infrastructure/SourceItemNormalizer.cs` — implementação de `IContentProcessor`; canonicalização de URL (lowercase scheme/host, remove fragmento, remove porta default, remove trailing slash, ordena query params); limpeza de texto (strip HTML, collapse whitespace, decode entidades HTML); hash SHA256 do conteúdo normalizado; validação de conteúdo vazio/curto/título vazio; itens inválidos vão para `Failed` com mensagem de erro; itens válidos ficam em `Processing`
  - `WhatsAppNewsPortal.Api/Sources/Infrastructure/EfSourceItemRepository.cs` — implementação EF Core de `ISourceItemRepository` (GetById com Include Source, ExistsByUrl, Add, Update com SaveChanges)
  - `WhatsAppNewsPortal.Api/Program.cs` — registro DI de `ISourceItemRepository → EfSourceItemRepository` e `IContentProcessor → SourceItemNormalizer`
  - `WhatsAppNewsPortal.Api.Tests/SourceItemNormalizerTests.cs` — 36 testes unitários com FakeSourceItemRepository
- **Testes criados/executados:**
  - `CanonicalizeUrl_LowercasesSchemeAndHost` — scheme e host em minúsculo
  - `CanonicalizeUrl_RemovesTrailingSlash` — trailing slash removido
  - `CanonicalizeUrl_KeepsRootSlash` — slash raiz mantido
  - `CanonicalizeUrl_RemovesFragment` — fragmento (#section) removido
  - `CanonicalizeUrl_RemovesDefaultHttpPort` — porta 80 removida
  - `CanonicalizeUrl_RemovesDefaultHttpsPort` — porta 443 removida
  - `CanonicalizeUrl_KeepsNonDefaultPort` — porta não-padrão mantida
  - `CanonicalizeUrl_SortsQueryParameters` — query params ordenados alfabeticamente
  - `CanonicalizeUrl_TrimsWhitespace` — whitespace removido
  - `CanonicalizeUrl_EmptyUrl_ReturnsEmpty` — URL vazia retorna string vazia
  - `CanonicalizeUrl_InvalidUri_ReturnsLowercased` — URI inválida retorna lowercased
  - `ComputeHash_ReturnsSha256Hex` — hash SHA256 correto (verificado contra valor conhecido)
  - `ComputeHash_DifferentInputs_DifferentHashes` — inputs diferentes geram hashes diferentes
  - `ComputeHash_SameInput_SameHash` — mesmo input gera mesmo hash
  - `CleanText_RemovesHtmlTags` — tags HTML removidas
  - `CleanText_CollapsesWhitespace` — múltiplos espaços colapsados
  - `CleanText_CollapsesNewlines` — newlines e tabs colapsados
  - `CleanText_DecodesHtmlEntities` — entidades HTML decodificadas (&amp; &lt; &gt; &quot; &#39;)
  - `CleanText_TrimsResult` — resultado trimado
  - `CleanText_NullOrWhitespace_ReturnsNull` — null/vazio retorna null
  - `CleanText_DecodesNbsp` — &nbsp; decodificado para espaço
  - `NormalizeAsync_ValidItem_SetsProcessingStatus` — item válido vai para Processing
  - `NormalizeAsync_ValidItem_SetsCanonicalUrl` — CanonicalUrl preenchida
  - `NormalizeAsync_ValidItem_ComputesContentHash` — ContentHash de 64 chars (SHA256 hex)
  - `NormalizeAsync_ValidItem_CleansAndPersistsNormalizedContent` — conteúdo limpo persistido
  - `NormalizeAsync_ValidItem_PersistsUpdate` — repository.UpdateAsync chamado
  - `NormalizeAsync_ValidItem_ReturnsDtoWithAllFields` — DTO com todos os campos preenchidos
  - `NormalizeAsync_EmptyContent_SetsFailedStatus` — conteúdo vazio → Failed
  - `NormalizeAsync_WhitespaceOnlyContent_SetsFailedStatus` — whitespace only → Failed
  - `NormalizeAsync_EmptyTitle_SetsFailedStatus` — título vazio → Failed
  - `NormalizeAsync_TooShortContent_SetsFailedStatus` — conteúdo < 20 chars → Failed
  - `NormalizeAsync_FailedItem_PersistsWithErrorMessage` — item falho persistido com ErrorMessage
  - `NormalizeAsync_FailedItem_ReturnsDtoWithEmptyContentAndHash` — DTO de falha com content/hash vazios
  - `NormalizeAsync_HtmlContent_StripsTagsBeforeHashing` — HTML stripped antes do hash
  - `NormalizeAsync_SameContent_ProducesSameHash` — mesmo conteúdo → mesmo hash
  - `NormalizeAsync_NullSource_DefaultsToOfficialSourceType` — Source null → SourceType.Official
  - Resultado: **107 aprovados, 0 falhas** (36 novos + 71 anteriores)
- **Validação manual:** `dotnet build` compila com 0 warnings/0 erros; `dotnet test` passa 107/107; normalizer registrado no DI como `IContentProcessor → SourceItemNormalizer`; repository registrado como `ISourceItemRepository → EfSourceItemRepository`; fluxo: SourceItem entra com status Discovered → normalizer canonicaliza URL, limpa texto, calcula hash → se válido, status vai para Processing e item é persistido; se inválido (conteúdo vazio, título vazio, conteúdo < 20 chars), status vai para Failed com ErrorMessage descritivo
- **Riscos/pendências:** nenhum; a deduplicação por canonicalUrl e contentHash será implementada na Tarefa 11; a integração com o orquestrador será feita na Tarefa 17
- **Data de conclusão:** 2026-03-28

---

## Tarefa 11 — Implementar deduplicação básica
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `Sources/Application/ISourceItemRepository.cs` — adicionados `ExistsByCanonicalUrlAsync` e `ExistsByContentHashAsync`
  - `Sources/Infrastructure/EfSourceItemRepository.cs` — implementados os dois novos métodos (queries EF com AnyAsync nas colunas indexadas)
  - `Articles/Application/IArticleRepository.cs` — adicionado `ExistsBySourceItemIdAsync`
  - `Articles/Infrastructure/EfArticleRepository.cs` — nova implementação EF Core de `IArticleRepository` (GetById, GetBySlug, GetPublished, ExistsBySourceItemId, Add, Update)
  - `ContentProcessing/Application/IDeduplicationService.cs` — interface + record `DeduplicationResult(IsDuplicate, Reason)`; métodos `CheckSourceItemAsync` (por canonicalUrl e/ou contentHash) e `ArticleExistsForSourceItemAsync`
  - `ContentProcessing/Infrastructure/DeduplicationService.cs` — implementação; verifica canonicalUrl primeiro, depois contentHash; logs em Debug; delega ao repositório correspondente
  - `Program.cs` — registro DI de `IDeduplicationService → DeduplicationService` e `IArticleRepository → EfArticleRepository`
  - `WhatsAppNewsPortal.Api.Tests/SourceItemNormalizerTests.cs` — `FakeSourceItemRepository` atualizada com os novos métodos da interface
  - `WhatsAppNewsPortal.Api.Tests/DeduplicationServiceTests.cs` — 11 novos testes (7 unit + 4 integration)
- **Testes criados/executados:**
  - `CheckSourceItem_NeitherUrlNorHash_ReturnsNotDuplicate` — sem URL nem hash → não duplicado
  - `CheckSourceItem_CanonicalUrlExists_ReturnsDuplicate` — URL existente → duplicado com reason
  - `CheckSourceItem_ContentHashExists_ReturnsDuplicate` — hash existente → duplicado com reason
  - `CheckSourceItem_UrlTakesPrecedenceOverHash` — URL verificada antes do hash
  - `CheckSourceItem_NoMatchOnEither_ReturnsNotDuplicate` — sem match → não duplicado
  - `ArticleExistsForSourceItem_WhenArticleExists_ReturnsTrue` — article existente para sourceItemId → true
  - `ArticleExistsForSourceItem_WhenNoArticle_ReturnsFalse` — sem article para sourceItemId → false
  - `Integration_CanonicalUrl_Deduplication_Works` — InMemory DB: item existente bloqueado por canonicalUrl
  - `Integration_ContentHash_Deduplication_Works` — InMemory DB: item existente bloqueado por contentHash
  - `Integration_ArticleDuplicate_Works` — InMemory DB: article vinculado detectado
  - `Integration_Reexecution_DoesNotCreateDuplicateSourceItem` — segunda execução detecta item já persistido
  - Resultado: **118 aprovados, 0 falhas** (11 novos + 107 anteriores)
- **Validação manual:** `dotnet build` compila com 0 warnings/0 erros; `dotnet test` passa 118/118; deduplicação funciona por canonicalUrl (primeiro) e contentHash (segundo); pipeline pode chamar `IDeduplicationService.CheckSourceItemAsync` antes de persistir novos SourceItems e `ArticleExistsForSourceItemAsync` antes de gerar artigo; razão do descarte fica disponível no `DeduplicationResult.Reason` para logging
- **Riscos/pendências:** nenhum; a chamada ao serviço de deduplicação será integrada ao orquestrador (Tarefa 17) — a presente tarefa expõe a interface e testa as regras isoladamente
- **Data de conclusão:** 2026-03-28

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
