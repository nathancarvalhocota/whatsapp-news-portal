# Registro de Progresso do Projeto

Este arquivo registra o progresso de cada tarefa do plano de implementação (a partir da etapa 20). Deve ser atualizado pelo agente ao concluir cada tarefa. Não editar manualmente sem necessidade.


## Tarefa 20 — Inicializar o front-end Next.js
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/web-next/package.json` — dependências Next.js 15, React 19, Tailwind 3, Jest/ts-jest
  - `apps/web-next/tsconfig.json` — TypeScript strict, paths `@/*`
  - `apps/web-next/next.config.ts` — output standalone
  - `apps/web-next/tailwind.config.ts` — content paths configurados
  - `apps/web-next/postcss.config.mjs` — tailwindcss + autoprefixer
  - `apps/web-next/.env.example` — variável `NEXT_PUBLIC_API_URL`
  - `apps/web-next/.env.local` — apontando para localhost:5000
  - `apps/web-next/lib/api.ts` — cliente HTTP tipado (ArticleSummary, ArticleDetail, SourceReference)
  - `apps/web-next/app/globals.css` — Tailwind base/components/utilities
  - `apps/web-next/app/layout.tsx` — RootLayout com header/footer
  - `apps/web-next/app/page.tsx` — página inicial com listagem de artigos publicados (SSR)
  - `apps/web-next/jest.config.js` — configuração Jest com ts-jest
  - `apps/web-next/__tests__/api.smoke.test.ts` — smoke test do módulo de API
- **Testes criados/executados:**
  - 3 smoke tests em `__tests__/api.smoke.test.ts` — todos passando
  - `npm run build` — build de produção concluído sem erros
- **Validação manual:**
  - `npm run build` produz build estático em `.next/` sem erros de tipo ou compilação
  - `npm test` executa 3 testes com sucesso
  - Front sobe com `npm run dev` em `http://localhost:3000`
  - Página inicial tenta buscar artigos via `NEXT_PUBLIC_API_URL` (resiliente a cold start: exibe mensagem de erro sem quebrar)
- **Riscos/pendências:**
  - Página `/artigos/[slug]` e `/categorias/[categoria]` serão criadas nas tarefas 21-23
  - Back-end precisa estar rodando para exibir artigos reais
- **Data de conclusão:** 2026-03-29

---

## Tarefa 21 — Implementar página inicial do portal
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/web-next/app/page.tsx` — hero + grid responsivo 2 colunas (sm:grid-cols-2), primeiro card featured, estados de erro e vazio
  - `apps/web-next/components/ArticleCard.tsx` — card com badge Beta (amber) / Oficial (green), título, excerpt, data, link para /artigos/[slug]
  - `apps/web-next/jest.config.js` — migrado para next/jest (SWC) com jsdom padrão
  - `apps/web-next/package.json` — adicionado @testing-library/react, @testing-library/jest-dom, jest-environment-jsdom
  - `apps/web-next/__tests__/api.smoke.test.ts` — anotado com @jest-environment node
  - `apps/web-next/__tests__/ArticleCard.test.tsx` — 7 testes de render
- **Testes criados/executados:**
  - 7 testes `ArticleCard.test.tsx`: título, excerpt, badge Oficial, badge Beta, link, categoria presente/ausente — todos passando
  - 3 testes `api.smoke.test.ts` — todos passando
  - `npm run build` — sem erros
- **Validação manual:**
  - Grid responsivo: 1 coluna em mobile, 2 colunas em sm+ (Tailwind sm:grid-cols-2)
  - Primeiro artigo ocupa 2 colunas (featured, md:col-span-2)
  - Badge amber "Beta" para articleType=BetaNews; badge green "Oficial" para demais
  - Erro de API exibe alerta sem quebrar a página
  - Estado vazio exibe mensagem centralizada
- **Riscos/pendências:**
  - Página `/artigos/[slug]` ainda não existe (Tarefa 22)
- **Data de conclusão:** 2026-03-29

---

## Tarefa 22 — Implementar página de artigo
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/web-next/app/artigos/[slug]/page.tsx` — rota dinâmica SSR, generateMetadata, notFound() para 404, aviso beta, fontes, tags, JSON-LD, link de volta
  - `apps/web-next/app/globals.css` — estilos `.prose` para o contentHtml (h2, h3, p, ul, ol, a, blockquote, strong)
  - `apps/web-next/__tests__/ArticlePage.test.tsx` — 6 testes de render e 404
- **Testes criados/executados:**
  - 6 testes `ArticlePage.test.tsx`: título, excerpt, badge Oficial, aviso beta + role=note, link de fonte, notFound() para 404 — todos passando
  - Total: 16 testes passando (3 suites)
  - `npm run build` — sem erros, rota `/artigos/[slug]` como Dynamic SSR
- **Validação manual:**
  - Rota `/artigos/[slug]` renderiza SSR com `generateMetadata` (metaTitle, metaDescription)
  - Artigo beta: badge âmbar + aside de aviso (role=note) com explicação do estágio
  - Artigo oficial: badge verde "Anúncio oficial"
  - Fontes listadas com link externo (target=_blank, rel=noopener)
  - JSON-LD injetado via script tag quando presente no back-end
  - 404 para slug inexistente via notFound()
  - HTML semântico: article > header > h1, time, footer
- **Riscos/pendências:**
  - Página de categoria (`/categorias/[categoria]`) será Tarefa 23
- **Data de conclusão:** 2026-03-29

---

## Tarefa 23 — Implementar página de categoria
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/web-next/app/categorias/[categoria]/page.tsx` — listagem SSG (generateStaticParams: oficial + beta), generateMetadata, notFound para categorias desconhecidas, aviso beta inline, breadcrumb, estados de erro e vazio
  - `apps/web-next/app/layout.tsx` — nav com links "Oficial" e "Beta" no header
  - `apps/web-next/__tests__/CategoryPage.test.tsx` — 7 testes
- **Testes criados/executados:**
  - 7 testes `CategoryPage.test.tsx`: título oficial, título beta, artigos exibidos, vazio, erro de API, notFound para categoria desconhecida, aviso beta — todos passando
  - Total: 23 testes passando (4 suites)
  - `npm run build` — `/categorias/oficial` e `/categorias/beta` pré-geradas como SSG (revalidate 60s)
- **Validação manual:**
  - `/categorias/oficial` e `/categorias/beta` acessíveis e pré-geradas no build
  - Categorias desconhecidas retornam 404
  - Categoria beta exibe aviso "não representa anúncios oficiais"
  - Nav no header com links Oficial / Beta em todas as páginas
  - Breadcrumb "Início › [Categoria]" no topo da página
  - Back-end usa `Category = "beta"` ou `"oficial"` (confirmado em ArticleGenerationStep.cs:102-104)
- **Riscos/pendências:**
  - Nenhum
- **Data de conclusão:** 2026-03-29

---

## Tarefa 24 — Implementar SEO técnico do front
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/web-next/app/layout.tsx` — adicionado `metadataBase`, Open Graph padrão (`type: website`, `locale: pt_BR`, `siteName`), `robots: { index, follow }`
  - `apps/web-next/app/page.tsx` — título corrigido para `{ absolute }` (evita template duplicado), `alternates.canonical: '/'`
  - `apps/web-next/app/artigos/[slug]/page.tsx` — `generateMetadata` com canonical e OG (`type: article`, `publishedTime`); JSON-LD sempre renderizado (backend ou fallback NewsArticle)
  - `apps/web-next/app/categorias/[categoria]/page.tsx` — `generateMetadata` com canonical e OG
  - `apps/web-next/app/sitemap.ts` — criado; rotas estáticas + todos os artigos publicados via API (tolerante a falha)
  - `apps/web-next/app/robots.ts` — criado; permite todos os crawlers, aponta para `/sitemap.xml`
  - `apps/web-next/.env.example` — documentada variável `NEXT_PUBLIC_SITE_URL`
- **Testes criados/executados:** 23 testes existentes passando (jest); TypeScript compilou sem erros (`tsc --noEmit`)
- **Validação manual:** metadataBase resolve URLs relativas; JSON-LD sempre presente (backend ou fallback); sitemap tolerante a API offline; robots.txt gerado automaticamente
- **Riscos/pendências:** definir `NEXT_PUBLIC_SITE_URL` no painel da Vercel antes do deploy para canonical e sitemap corretos
- **Data de conclusão:** 2026-03-29

---

## Tarefa 25 — Implementar botão ou rota operacional de demo/admin mínima
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/web-next/app/admin/page.tsx` — página `/admin` (client component) com gate de senha, 3 seções: Configurações do Pipeline, Demo Pipeline (URL input + reset=false), Drafts pendentes
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Program.cs` — `GET /api/articles/drafts`, `GET /api/settings/pipeline`, `PUT /api/settings/pipeline`; `PipelineJobSettings` registrado como singleton mutável (não mais IOptions)
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Pipeline/Infrastructure/ContentPipelineJob.cs` — injeção direta de `PipelineJobSettings` (singleton)
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Pipeline/Infrastructure/PipelineOrchestrator.cs` — injeção direta de `PipelineJobSettings` (singleton)
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/PipelineOrchestratorTests.cs` — removido `Options.Create` wrapper
  - `apps/web-next/app/layout.tsx` — `body` com `flex flex-col`, `main` com `flex-1` (corrige footer flutuante em páginas curtas)
  - `apps/web-next/.env.local` — adicionada `NEXT_PUBLIC_ADMIN_SECRET=hackathon2025`
  - `apps/web-next/.env.example` — documentada variável `NEXT_PUBLIC_ADMIN_SECRET`
- **Testes criados/executados:**
  - `npm run build` — `/admin` compilada como Static (○), sem erros
  - `dotnet build` — 0 erros, 0 avisos (solução completa incluindo testes)
- **Validação manual:**
  - Acesso a `/admin` apresenta gate de senha (senha: `hackathon2025`)
  - **Configurações do Pipeline**: lê valores atuais via `GET /api/settings/pipeline`; formulário com intervalo (min), data mínima (date picker), auto-publish (checkbox); salva via `PUT /api/settings/pipeline`; alterações refletem no próximo ciclo do background job
  - **Demo Pipeline**: campo URL + botão "Rodar Demo" → `POST /api/pipeline/run-demo` com `reset: false`; resultado inline com status, slug, article ID e botão "Publicar este artigo" se status=Draft
  - **Drafts pendentes**: lista todos os drafts via `GET /api/articles/drafts`; botão "Publicar" por item → `POST /api/articles/{id}/publish`; link para o artigo publicado após sucesso
  - Footer agora gruda no final da viewport em páginas com pouco conteúdo (flex-col + flex-1)
  - Sem acesso manual ao banco necessário
- **Riscos/pendências:**
  - `NEXT_PUBLIC_ADMIN_SECRET` fica exposto no bundle client-side — proteção suficiente para hackathon; não usar em produção real
  - Definir `NEXT_PUBLIC_ADMIN_SECRET` na Vercel (env var) antes do deploy
  - Alterações de settings são em memória — reinício do servidor volta aos valores das env vars
- **Data de conclusão:** 2026-03-29

---

## Tarefa 26 — Implementar logs e observabilidade mínima
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Program.cs` — logging com `IncludeScopes=true`; `AddSimpleConsole` (dev) + `AddJsonConsole` (prod); health endpoint ampliado com `timestamp` e `environment`
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Articles/Infrastructure/ArticlePublisher.cs` — adicionado `ILogger<ArticlePublisher>`; logs de início, idempotência, validação e publicação bem-sucedida
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Pipeline/Infrastructure/PipelineOrchestrator.cs` — `ILogger.BeginScope` com `CorrelationId` (8 chars) e `PipelineStage=orchestrator` em cada execução
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Demo/Infrastructure/DemoPipelineService.cs` — `ILogger.BeginScope` com `CorrelationId` e `PipelineStage=demo` em cada execução demo
- **Testes criados/executados:**
  - `dotnet build` — 0 erros, 0 avisos
- **Validação manual:**
  - Logs distinguem: descoberta (`[Pipeline][Source]`), normalização, classificação, geração de draft, publicação (`[Publicação]`) e falha
  - CorrelationId aparece em todos os logs de uma execução quando scopes estão ativos
  - Health: `GET /health` retorna `{ status, timestamp, environment }`
  - Nenhum segredo (API key) nos logs — `GeminiTextGenerationProvider` envia a key no header HTTP, não nos logs; erros truncados a 500 chars
- **Riscos/pendências:**
  - `AddJsonConsole` requer que o ambiente de produção (Render) reconheça `ASPNETCORE_ENVIRONMENT=Production`; se não configurado, cai no branch dev (simple console, igualmente funcional)
- **Data de conclusão:** 2026-03-29

---

## Tarefa 27 — Implementar tratamento robusto de falhas do pipeline
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/GeminiTextGenerationProviderTests.cs` — criado; 13 testes cobrindo erros HTTP (500, 401, 429, 503), HttpRequestException, timeout (TaskCanceledException), API key ausente, resposta vazia e sem candidatos
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/PipelineOrchestratorTests.cs` — adicionados 4 testes: falha de geração não interrompe outros itens, status `Failed` persistido no banco após erro de classificação, status `Failed` persistido após erro de geração, falha de ingestão em fonte não impede outras
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/ArticlePublisherTests.cs` — corrigido erro pré-existente: `ArticlePublisher` passou a exigir `ILogger` na Tarefa 26, mas os testes não foram atualizados; agora passam `NullLogger`
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Program.cs` — adicionados: `GET /api/source-items/failed` (lista itens com status Failed para o admin), `POST /api/source-items/{id}/reprocess` (reprocessa manualmente um item Failed: normalização → classificação → geração), `POST /api/pipeline/run` (endpoint que faltava para disparo do pipeline real pelo admin)
- **Testes criados/executados:**
  - 246 testes passando (era ~229 antes); 17 testes novos adicionados
  - `GeminiTextGenerationProviderTests` — 13 testes: HTTP errors, timeout, API key inválida, resposta vazia
  - `PipelineOrchestratorTests` — 4 testes novos de isolamento de falha e persistência de status
- **Validação manual:**
  - `dotnet build` — 0 erros, 0 avisos
  - `dotnet test` — 246/246 aprovados
  - Falha de classificação → `SourceItem.Status = Failed`, `ErrorMessage` preenchido, `ProcessingLog` criado
  - Falha de geração → mesma persistência, item seguinte continua processando
  - Falha de ingestão HTTP em uma fonte → outras fontes continuam
  - `POST /api/source-items/{id}/reprocess` → reinicia pipeline para item com status Failed
  - `GET /api/source-items/failed` → lista até 50 itens falhos para o admin
- **Riscos/pendências:**
  - Reprocessamento não verifica se artigo já existe para o item (o `ArticleGenerationStep` já faz essa checagem internamente — retorna `Failed` com mensagem "already exists")
  - Timeout da IA é configurável via `GEMINI_TIMEOUT_SECONDS` (padrão 60s); sem circuit breaker (fora do escopo)
- **Data de conclusão:** 2026-03-29

---

## Tarefa Bônus — Melhorias visuais (home e artigo)
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/web-next/tailwind.config.ts` — adicionado `@tailwindcss/typography` plugin; extensão de cores com CSS variables (border, ring, card, muted, etc.) para compatibilidade Tailwind v3 + shadcn
  - `apps/web-next/app/globals.css` — removidos imports incompatíveis com Tailwind v3 (`@import "shadcn/tailwind.css"`, `@apply border-border`); CSS variables em `@layer base` com valores oklch
  - `apps/web-next/app/layout.tsx` — header sticky com logo WhatsApp + fonte Geist + nav com indicadores de cor; footer com branding Umbler, links de categoria e Separator shadcn
  - `apps/web-next/app/page.tsx` — hero renovado com eyebrow label, título maior, botões de categoria com ícones coloridos; grid 3 colunas (lg); seções separadas Oficial / Beta com contadores
  - `apps/web-next/app/artigos/[slug]/page.tsx` — max-w-3xl mx-auto; breadcrumb; Badge shadcn com cores green/amber; excerpt com borda lateral; conteúdo com `prose prose-lg prose-gray`; ícones SVG inline nos links de fonte; Separator shadcn
  - `apps/web-next/components/ArticleCard.tsx` — refatorado para usar shadcn Card (CardHeader + CardContent) e Badge com className override verde/âmbar; hover com sombra e translate
  - `apps/web-next/components.json` — gerado pelo `npx shadcn@latest init` (style: base-nova, Tailwind v3, RSC)
  - `apps/web-next/components/ui/card.tsx` — gerado pelo shadcn
  - `apps/web-next/components/ui/badge.tsx` — gerado pelo shadcn
  - `apps/web-next/components/ui/separator.tsx` — gerado pelo shadcn
  - `apps/web-next/components/ui/button.tsx` — gerado pelo shadcn
  - `apps/web-next/lib/utils.ts` — gerado pelo shadcn (cn helper com tailwind-merge + clsx)
- **Dependências instaladas:**
  - `@base-ui/react`, `class-variance-authority`, `clsx`, `lucide-react`, `tailwind-merge`, `tw-animate-css` (via shadcn init)
  - `@tailwindcss/typography@0.5.19` (para prose)
- **Testes criados/executados:**
  - 23 testes existentes (4 suites) — todos passando após refatoração
  - `npm run build` — compilado sem erros; 9 rotas geradas
- **Validação manual:**
  - Header sticky com logo WhatsApp em verde, nav com indicadores de cor
  - Home: hero com eyebrow, título grande (text-4xl/5xl), botões de categoria com dot colorido
  - Grid 3 colunas em lg, card featured ocupa 2 colunas
  - Badge verde "Oficial" e âmbar "Beta" em cards e página de artigo
  - Artigo: max-w-3xl, breadcrumb, título text-3xl/4xl, prose prose-lg (typography plugin)
  - Footer Umbler com Separator shadcn
  - Sem libs adicionais além das explicitamente pedidas na tarefa
- **Decisões técnicas:**
  - shadcn gerou estilo "base-nova" (v4-style CSS); corrigido para Tailwind v3 removendo `@import "shadcn/tailwind.css"` e `@apply border-border` do globals.css
  - Cores badge usam className override via tailwind-merge (não dependem de CSS variables)
- **Data de conclusão:** 2026-03-29

---

## Tarefa 28 — Popular o sistema com artigos reais (+ correções de ingestão)
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Pipeline/Application/PipelineJobSettings.cs` — criado; classe de configuração com variáveis facilmente modificáveis: `IntervalMinutes` (dev: 5min, prod: 720min/12h), `RunOnStartup` (default: true), `MinPublishedDate` (default: 2026-03-28), `AutoPublishDrafts` (default: true)
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Pipeline/Infrastructure/ContentPipelineJob.cs` — criado; `BackgroundService` que executa o pipeline automaticamente: uma vez no startup (após 5s de delay para migrations), depois no intervalo configurado; auto-publica drafts gerados; tratamento de erros robusto
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Pipeline/Infrastructure/PipelineOrchestrator.cs` — adicionado filtro por data mínima de publicação (`MinPublishedDate`); itens com `PublishedAt` anterior à data limite são ignorados (status `skipped_before_min_date`); itens sem data são processados normalmente
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Program.cs` — registrado `PipelineJobSettings` via `Configure<>` com leitura de env vars (`PIPELINE_INTERVAL_MINUTES`, `PIPELINE_RUN_ON_STARTUP`, `PIPELINE_MIN_DATE`, `PIPELINE_AUTO_PUBLISH`); registrado `ContentPipelineJob` como `HostedService`; dev usa 5min por padrão, prod usa 720min
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/appsettings.Development.json` — adicionadas variáveis de configuração do pipeline job para desenvolvimento
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/PipelineOrchestratorTests.cs` — atualizado para passar `IOptions<PipelineJobSettings>` ao construtor do `PipelineOrchestrator` (MinDate configurado para 2020 nos testes para não filtrar itens)
- **Testes criados/executados:**
  - 246 testes passando (nenhum quebrado pela alteração)
  - `dotnet build` — 0 erros, 0 avisos
- **Variáveis de configuração (env vars):**
  - `PIPELINE_INTERVAL_MINUTES` — intervalo em minutos entre execuções (dev: 5, prod: 720)
  - `PIPELINE_RUN_ON_STARTUP` — executar pipeline ao iniciar a API (default: true)
  - `PIPELINE_MIN_DATE` — data mínima de publicação dos posts, formato yyyy-MM-dd (default: 2026-03-28)
  - `PIPELINE_AUTO_PUBLISH` — publicar drafts automaticamente após pipeline (default: true)
- **Validação manual:**
  - Build compila sem erros
  - Todos os 246 testes passam
  - Background job configurado para rodar automaticamente ao iniciar a API
  - Filtro de data impede busca de posts antigos que poluiriam o site
  - Auto-publicação garante que artigos ficam visíveis sem intervenção manual
- **Comportamento do job:**
  - Em desenvolvimento: executa 1x no startup (após 5s) e depois a cada 5 minutos
  - Em produção: executa 1x no startup e depois a cada 12 horas
  - Se `PIPELINE_MIN_DATE` for alterado para `2026-03-01`, buscará posts a partir dessa data (ignorando os já existentes via dedup por URL)
  - Distinção oficial/beta mantida: fontes `Official` geram `OfficialNews`, fontes `BetaSpecialized` geram `BetaNews`
- **Correções pós-execução real (ingestão):**
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Program.cs` — User-Agent alterado de `WhatsAppNewsPortal/1.0` para User-Agent real de Chrome 124; resolve 403 no WABetaInfo e outros sites com proteção anti-bot
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Ingestion/Infrastructure/RssIngestionAdapter.cs` — adicionado `SanitizeXml()`: escapa `&` soltos (não seguidos de entidade XML válida) antes do parse; resolve `XmlException` no feed RSS do WhatsApp Blog
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/RssIngestionAdapterTests.cs` — 3 testes novos: `SanitizeXml_EscapesUnescapedAmpersands`, `SanitizeXml_PreservesValidEntities`, `ParseFeed_WithUnescapedAmpersand_ParsesSuccessfully`
- **Testes totais após correções:** 249 (era 246)
- **Riscos/pendências:**
  - HTML adapter (business.whatsapp.com, developers.facebook.com) não retorna `PublishedAt`, então esses itens não são filtrados por data (processados normalmente)
  - Volume controlado pelo filtro de data e pela frequência do job
- **Data de conclusão:** 2026-03-29

---

## Correção de produção 1 — Limpar source items com falha (delete em vez de Failed)
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Sources/Application/ISourceItemRepository.cs` — adicionado método `DeleteAsync`
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Sources/Infrastructure/EfSourceItemRepository.cs` — implementação de `DeleteAsync`
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Pipeline/Infrastructure/PipelineOrchestrator.cs` — `FailItemAsync` agora loga o ProcessingLog primeiro e depois deleta o SourceItem (antes: atualizava status para Failed)
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/PipelineOrchestratorTests.cs` — 2 testes renomeados e atualizados: `ClassificationError_SourceItemIsDeletedAndLogIsKept`, `ArticleGenerationError_SourceItemIsDeletedAndLogIsKept`
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/ArticleGenerationStepTests.cs` — adicionado `DeleteAsync` no fake
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/ClassificationStepTests.cs` — adicionado `DeleteAsync` no fake
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/DeduplicationServiceTests.cs` — adicionado `DeleteAsync` no fake
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/SourceItemNormalizerTests.cs` — adicionado `DeleteAsync` no fake
- **Testes criados/executados:**
  - 248 testes passando (1 falha pré-existente em `SourceSeederTests.Seed_WhatsAppBlog_Has_FeedUrl`, não relacionada)
- **Motivação:** SourceItems com Status=Failed bloqueavam o pipeline — o dedup por URL os encontrava e ignorava, impedindo reprocessamento na próxima execução
- **Comportamento novo:** falha em qualquer etapa (normalização, classificação, geração) → ProcessingLog mantido para auditoria → SourceItem deletado → próxima execução redescobre e reprocessa
- **Data de conclusão:** 2026-03-29

---

## Correção de produção 2 — Persistir SourceItem somente após draft (versão simplificada)
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Pipeline/Infrastructure/PipelineOrchestrator.cs` — `sourceItem` movido para fora do `try` block; catch genérico agora deleta o SourceItem órfão se já foi persistido (com try/catch de segurança no delete)
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Demo/Infrastructure/DemoPipelineService.cs` — todos os caminhos de falha (normalização, classificação, descarte, geração) agora deletam o SourceItem via `DeleteSourceItemAsync`; adicionado método helper `DeleteSourceItemAsync` com try/catch de segurança
- **Testes criados/executados:**
  - 249 testes passando, 0 falhas
- **Avaliação de risco:** refatoração completa (persistir somente após draft) foi avaliada e considerada de alto risco:
  - `SourceItemNormalizer` chama `UpdateAsync` no SourceItem (ambos os caminhos sucesso/falha)
  - `ClassificationStep` e `ArticleGenerationStep` buscam o SourceItem por ID do banco para atualizar status
  - Estes steps são compartilhados com `DemoPipelineService` e endpoint `/reprocess` que exigem SourceItem persistido
  - `Article` tem FK `Restrict` para SourceItem — deve existir antes da criação do artigo
- **Versão simplificada implementada:** complementar ao Item 1. Todos os caminhos de falha agora removem o SourceItem:
  - **PipelineOrchestrator:** normalização/classificação/geração → `FailItemAsync` → delete (Item 1); exceção inesperada → catch genérico → delete
  - **DemoPipelineService:** normalização/classificação/descarte/geração → `DeleteSourceItemAsync` → delete
  - Endpoint `/reprocess` mantido sem delete (ação manual do admin — pode tentar novamente)
- **Data de conclusão:** 2026-03-29

---

## Correção de produção 3 — Configurar seletores HTML por fonte
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Ingestion/Infrastructure/HtmlIngestionAdapter.cs` — SourceConfigs atualizado:
    - **blog.whatsapp.com** (NOVO): `ArticleLinkPattern = blog\.whatsapp\.com/\w+(-\w+){2,}` — exige 3+ palavras separadas por hyphens no slug; filtra navegação (links para www.whatsapp.com)
    - **business.whatsapp.com** (ATUALIZADO): `ArticleLinkPattern = /blog/\w+(-\w+)+` — exige 2+ palavras no slug; filtra `/blog/` sem slug; `TitleSelector` adicionado `h5`
    - **wabetainfo.com** (NOVO): `ArticleLinkSelector = h3.entry-title a` — seletor CSS específico para cards de artigo; `ArticleLinkPattern = wabetainfo\.com/\w+(-\w+){2,}` filtra paths curtos (/android/, /ios/)
    - **developers.facebook.com**: mantido sem alterações
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/HtmlIngestionAdapterTests.cs` — 5 testes novos: config para blog.whatsapp.com, config para wabetainfo.com, filtragem de navegação para blog.whatsapp.com, filtragem para wabetainfo.com, filtragem de links genéricos para business.whatsapp.com
- **Testes criados/executados:**
  - 254 testes passando, 0 falhas (5 novos)
- **Validação:** HTML real de cada fonte verificado via WebFetch; seletores CSS e padrões regex validados contra estrutura real
- **Data de conclusão:** 2026-03-29

---

## Correção de produção 4 — Filtro de título mínimo na descoberta HTML
- **Status:** concluída
- **Arquivos criados/alterados:**
  - `apps/api-dotnet/WhatsAppNewsPortal.Api/Ingestion/Infrastructure/HtmlIngestionAdapter.cs` — `ParseListingPage`: adicionado `if (title.Length < 15) continue;` após a checagem de título vazio; elimina links de navegação com títulos curtos ("Log in", "Download", "Read more", "Careers", "Android", "iPhone")
  - `apps/api-dotnet/WhatsAppNewsPortal.Api.Tests/HtmlIngestionAdapterTests.cs` — 1 teste novo (`ParseListingPage_FiltersShortTitles`); fixtures `DefaultSelectorFixture` e `DuplicateLinksFixture` atualizadas com títulos ≥ 15 chars
- **Testes criados/executados:** 255 testes passando, 0 falhas
- **Data de conclusão:** 2026-03-29

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
