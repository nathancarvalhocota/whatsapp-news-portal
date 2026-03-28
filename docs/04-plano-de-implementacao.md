# 04 — Plano de Implementação

## Objetivo deste documento

Este documento define a ordem de implementação do projeto, com granularidade adequada para agentes de codificação trabalharem **uma tarefa por vez**, de forma incremental, segura e verificável.

Este documento **não é um backlog genérico**. Ele é uma sequência operacional de implementação com:
- ordem obrigatória de execução;
- dependências entre tarefas;
- objetivo técnico de cada tarefa;
- resultado esperado;
- critérios de aceite;
- testes mínimos exigidos;
- observações para evitar desvios de escopo.

## Regras de uso deste plano

1. O agente deve executar **uma tarefa por vez**.
2. O agente **não deve antecipar tarefas futuras** sem necessidade explícita.
3. O agente **não deve redesenhar a arquitetura** definida nos documentos anteriores.
4. Toda tarefa deve terminar com:
   - código compilando;
   - testes aplicáveis passando;
   - validação manual descrita;
   - resumo dos arquivos alterados;
   - riscos ou pendências remanescentes.
5. Quando uma tarefa mencionar “não implementar agora”, isso deve ser respeitado rigidamente.
6. O foco é o **mínimo funcional ponta a ponta**, com qualidade suficiente para demo pública do hackathon.

---

# 1. Visão geral da ordem de execução

A implementação deve seguir esta macro-ordem:

1. Estrutura do repositório e convenções
2. Back-end base (.NET API)
3. Banco de dados e persistência
4. Modelagem do pipeline editorial
5. Integração com Gemini
6. Ingestão de fontes
7. Pipeline manual e modo demo
8. Publicação de artigos
9. Front-end público em Next.js
10. SEO técnico
11. Refinos editoriais e UX mínima
12. Observabilidade mínima
13. Deploy
14. Seed/dados reais para demo
15. Validação final ponta a ponta

A sequência acima é obrigatória, porque reduz retrabalho e evita que o front-end seja construído sobre contratos ainda instáveis.

---

# 2. Convenções globais de implementação

## 2.1 Convenções de nomes

### Status do pipeline
Os estados mínimos do conteúdo devem ser:
- `discovered`
- `processing`
- `draft`
- `published`
- `failed`

### Tipos de fonte
Os tipos mínimos devem ser:
- `official`
- `beta_specialized`

### Tipo editorial do artigo
Os tipos mínimos devem ser:
- `official_news`
- `beta_news`

## 2.2 Convenções de segurança e robustez

- Não logar chaves de API.
- Não logar corpo integral gerado pela IA em logs normais.
- Erros externos devem ser persistidos de forma resumida e segura.
- Toda operação de publicação deve ser idempotente.
- Toda chamada à IA deve ter timeout explícito.
- Toda integração externa deve ser encapsulada em adapter/interface.

## 2.3 Convenções de implementação por camada

### Back-end
- ASP.NET Core Web API
- organização por domínio/módulo, não por tipo genérico apenas
- EF Core + PostgreSQL
- separação entre aplicação, domínio, infraestrutura e API, mesmo que leve

### Front-end
- Next.js App Router
- TypeScript
- páginas públicas focadas em SSR/SEO
- UI simples e responsiva

---

# 3. Estrutura inicial sugerida do repositório

O projeto pode ser monorepo ou multi-repo. Para velocidade no hackathon, recomenda-se **monorepo simples**.

Estrutura sugerida:

```text
/portal-whatsapp-news
  /apps
    /web-next
    /api-dotnet
  /docs
  /samples
  /scripts
```

### Diretrizes
- `apps/web-next`: front-end público
- `apps/api-dotnet`: API, pipeline, integrações, publicação
- `docs`: documentos do projeto
- `samples`: fixtures HTML/JSON para modo demo
- `scripts`: apoio local e deploy

---

# 4. Tarefas em ordem de execução

## Tarefa 01 — Criar a estrutura base do repositório

### Objetivo
Criar a base física do projeto para permitir desenvolvimento paralelo controlado entre back-end e front-end.

### Implementar
- criar a estrutura de pastas do monorepo;
- criar arquivos README mínimos por aplicação;
- criar `.gitignore` adequado;
- criar arquivo `.editorconfig`;
- criar convenções básicas de ambiente (`.env.example` para front e back);
- criar pasta `samples` para fixtures do modo demo.

### Resultado esperado
O repositório deve estar organizado e pronto para receber as aplicações sem ambiguidade estrutural.

### Critérios de aceite
- estrutura criada conforme convenção;
- arquivos de ambiente de exemplo presentes;
- sem arquivos temporários desnecessários;
- documentação mínima explicando o propósito de cada app.

### Testes mínimos
- validação manual da estrutura.

### Não fazer agora
- automações complexas de workspace;
- pipelines CI/CD completos.

---

## Tarefa 02 — Inicializar a aplicação back-end .NET

### Objetivo
Criar a base da API em ASP.NET Core que servirá como núcleo do sistema.

### Implementar
- criar projeto ASP.NET Core Web API;
- configurar leitura de variáveis de ambiente;
- configurar health endpoint (`/health`);
- configurar logging estruturado básico;
- configurar CORS para consumo do front-end;
- criar controller de teste simples;
- criar projeto(s) de teste associados.

### Resultado esperado
A API deve subir localmente, responder health check e estar preparada para crescer de forma organizada.

### Critérios de aceite
- aplicação sobe sem erros;
- endpoint `/health` responde 200;
- configuração por ambiente funcionando;
- testes do projeto criados e executáveis.

### Testes mínimos
- unit test simples de smoke do ambiente, se aplicável;
- integração mínima validando `/health`.

### Não fazer agora
- autenticação;
- autorização;
- workers distribuídos separados.

---

## Tarefa 03 — Definir a estrutura interna do back-end

### Objetivo
Estabelecer organização de código sustentável para evitar acoplamento e crescimento caótico.

### Implementar
Estrutura mínima sugerida:
- `Domain`
- `Application`
- `Infrastructure`
- `Api`

Ou, se preferir simplificar no hackathon:
- pastas por módulo dentro da API, mantendo interfaces claras.

Módulos mínimos a prever:
- `Sources`
- `Ingestion`
- `ContentProcessing`
- `AiGeneration`
- `Articles`
- `Demo`
- `Common`

### Resultado esperado
O projeto deve ter limites de responsabilidade claros.

### Critérios de aceite
- cada módulo com propósito claro;
- adapters externos isolados;
- entidades e DTOs não misturados de forma descontrolada.

### Testes mínimos
- validação manual da estrutura.

### Não fazer agora
- abstrações excessivas;
- patterns desnecessários para um hackathon.

---

## Tarefa 04 — Configurar PostgreSQL e EF Core

### Objetivo
Habilitar persistência real do sistema.

### Implementar
- instalar/configurar Npgsql + EF Core;
- configurar connection string por ambiente;
- criar DbContext;
- habilitar migrations;
- criar primeira migration.

### Resultado esperado
O back-end deve persistir dados em PostgreSQL local/remoto.

### Critérios de aceite
- migration criada com sucesso;
- aplicação conecta ao banco;
- database update funciona.

### Testes mínimos
- integração com banco real de desenvolvimento ou container local;
- teste simples de persistência/leitura.

### Não fazer agora
- otimizações avançadas;
- múltiplos bancos.

---

## Tarefa 05 — Modelar entidades principais do domínio

### Objetivo
Definir o núcleo de dados do pipeline editorial.

### Implementar
Entidades mínimas:
- `Source`
- `SourceItem`
- `Article`
- `ArticleSourceReference` (ou equivalente)
- `ProcessingLog` ou `PipelineExecution`

### Campos mínimos esperados

#### Source
- id
- name
- type (`official` ou `beta_specialized`)
- baseUrl
- feedUrl opcional
- isActive
- createdAt
- updatedAt

#### SourceItem
- id
- sourceId
- originalUrl
- canonicalUrl
- title
- rawContent
- normalizedContent
- publishedAt
- discoveredAt
- contentHash
- status
- sourceClassification
- isDemoItem
- errorMessage opcional
- createdAt
- updatedAt

#### Article
- id
- sourceItemId
- slug
- title
- excerpt
- contentHtml ou contentMarkdown
- metaTitle
- metaDescription
- schemaJsonLd
- category
- tags serializadas ou relacionamento simples
- articleType (`official_news` ou `beta_news`)
- status (`draft` ou `published`)
- publishedAt opcional
- createdAt
- updatedAt

#### ArticleSourceReference
- id
- articleId
- sourceName
- sourceUrl
- referenceType

#### ProcessingLog / PipelineExecution
- id
- sourceItemId opcional
- stepName
- status
- message resumida
- createdAt

### Resultado esperado
Modelo suficiente para ingestão, tratamento, geração, draft e publicação.

### Critérios de aceite
- entidades mapeadas;
- migration gerada;
- tipos e relacionamentos coerentes;
- status modelados corretamente.

### Testes mínimos
- integração validando persistência das entidades.

### Não fazer agora
- taxonomia complexa com dezenas de tabelas;
- versionamento editorial completo.

---

## Tarefa 06 — Criar seed das fontes oficiais e da fonte beta especializada

### Objetivo
Garantir que o sistema tenha as fontes base do hackathon carregadas de forma previsível.

### Implementar
Cadastrar as fontes:
- WhatsApp Blog
- WhatsApp Business Blog
- documentação oficial da API
- WABetaInfo

Cada fonte deve ter:
- nome legível;
- tipo correto;
- base URL;
- feed URL quando aplicável.

### Resultado esperado
O sistema deve iniciar já conhecendo as fontes suportadas.

### Critérios de aceite
- seed idempotente;
- fontes carregadas corretamente;
- WABetaInfo classificada como `beta_specialized`.

### Testes mínimos
- integração validando seed;
- idempotência do seed.

### Não fazer agora
- CRUD completo de fontes.

---

## Tarefa 07 — Definir contratos internos do pipeline

### Objetivo
Evitar acoplamento implícito entre etapas do pipeline.

### Implementar
Criar DTOs/contratos para:
- descoberta de item;
- normalização de item;
- classificação editorial;
- geração de draft;
- publicação de artigo;
- execução do modo demo.

### Resultado esperado
O pipeline deve operar com estruturas previsíveis.

### Critérios de aceite
- DTOs claros e versionáveis;
- sem uso indiscriminado de objetos anônimos;
- nomes sem ambiguidades.

### Testes mínimos
- unit tests de validação/serialização quando aplicável.

### Não fazer agora
- mensageria distribuída real.

---

## Tarefa 08 — Implementar adapter de ingestão por RSS

### Objetivo
Buscar novos itens de fontes que disponibilizam feed.

### Implementar
- interface para ingestão por fonte;
- adapter RSS;
- leitura de feed;
- extração de título, link, data e resumo quando houver;
- deduplicação preliminar por URL.

### Resultado esperado
O sistema deve conseguir consultar um feed e retornar candidatos a `SourceItem`.

### Critérios de aceite
- adapter desacoplado;
- falha de fonte não derruba o processo inteiro;
- URLs normalizadas minimamente.

### Testes mínimos
- testes com feed fixture;
- testes de parsing;
- testes de falha controlada.

### Não fazer agora
- heurísticas complexas de feed.

---

## Tarefa 09 — Implementar adapter de ingestão por HTML simples

### Objetivo
Cobrir fontes cujo processo exija leitura direta da página.

### Implementar
- interface para fetch de HTML;
- adapter HTTP com timeout e user-agent definido;
- extração do corpo bruto da página;
- parser inicial por seletores configuráveis ou estratégia simples.

### Resultado esperado
O sistema deve conseguir obter HTML e texto bruto de páginas suportadas.

### Critérios de aceite
- timeouts explícitos;
- tratamento de erro por fonte;
- corpo bruto persistível.

### Testes mínimos
- testes com fixtures HTML reais salvas em `samples`;
- testes de parser por fonte.

### Não fazer agora
- Playwright;
- scraping dinâmico complexo.

---

## Tarefa 10 — Implementar normalização de SourceItem

### Objetivo
Transformar conteúdo bruto em material consistente para passar pela IA.

### Implementar
- normalização de URL canônica;
- cálculo de hash do conteúdo;
- limpeza básica de texto;
- remoção de excesso de whitespace;
- validações mínimas de conteúdo vazio/inválido;
- persistência do `SourceItem` em status apropriado.

### Resultado esperado
Cada item descoberto deve virar um `SourceItem` coerente e reaproveitável.

### Critérios de aceite
- canonicalUrl consistente;
- hash gerado;
- conteúdo limpo não vazio;
- item inválido vai para `failed` com motivo.

### Testes mínimos
- unit tests de canonicalização;
- unit tests de hash;
- integração persistindo item normalizado.

### Não fazer agora
- deduplicação por embedding.

---

## Tarefa 11 — Implementar deduplicação básica

### Objetivo
Evitar duplicidade de itens e artigos no hackathon.

### Implementar
Regras mínimas:
- deduplicar por `canonicalUrl`;
- deduplicar por `contentHash`;
- impedir gerar novo artigo se já houver artigo vinculado ao mesmo item lógico.

### Resultado esperado
O pipeline não deve publicar o mesmo conteúdo mais de uma vez.

### Critérios de aceite
- reexecução do pipeline não duplica item/artigo;
- itens repetidos são ignorados ou marcados adequadamente.

### Testes mínimos
- unit tests das regras;
- integração reexecutando processamento.

### Não fazer agora
- clusterização semântica avançada.

---

## Tarefa 12 — Implementar cliente Gemini com abstração própria

### Objetivo
Integrar a IA sem acoplar o projeto diretamente à implementação concreta.

### Implementar
- interface `ITextGenerationProvider` ou equivalente;
- implementação Gemini;
- configuração por environment variables;
- métodos separados para:
  - classificação/metadata;
  - geração de artigo final;
- timeout;
- tratamento de falhas;
- logs seguros.

### Resultado esperado
O sistema deve chamar Gemini de forma controlada e reutilizável.

### Critérios de aceite
- provider isolado;
- sem chave hardcoded;
- tratamento de erro claro;
- respostas parseadas com segurança.

### Testes mínimos
- testes unitários com mock do provider;
- testes de parsing da resposta estruturada;
- evitar teste de integração real dependente da API em cada execução local.

### Não fazer agora
- fallback para outro provedor;
- múltiplos modelos por feature além dos já definidos.

---

## Tarefa 13 — Implementar etapa de classificação e metadata com Gemini Flash-Lite

### Objetivo
Usar IA para preparar o conteúdo editorial antes da geração do artigo.

### Implementar
Entrada: `SourceItem` normalizado.

Saída estruturada mínima:
- relevância do item;
- classificação editorial;
- indicação de tipo (`official_news` ou `beta_news`);
- resumo curto;
- slug sugerido;
- meta title;
- meta description;
- tags;
- observação editorial curta.

Regras obrigatórias:
- itens de fonte `beta_specialized` devem resultar em `beta_news`;
- não tratar conteúdo beta como lançamento oficial;
- usar saída estruturada validável.

### Resultado esperado
O item deve sair apto para virar draft.

### Critérios de aceite
- saída parseável e validada;
- conteúdo irrelevante pode ser descartado com registro;
- WABetaInfo sempre rotulado como beta.

### Testes mínimos
- golden tests com fixtures reais;
- testes de validação do JSON retornado;
- testes da regra beta.

### Não fazer agora
- avaliação semântica avançada;
- classificação muito granular.

---

## Tarefa 14 — Implementar etapa de geração de artigo final com Gemini Flash

### Objetivo
Gerar o texto final em PT-BR a partir do material classificado.

### Implementar
A geração deve produzir conteúdo:
- original;
- em português do Brasil;
- contextualizado para público brasileiro;
- não traduzido literalmente;
- com valor editorial;
- com distinção clara entre notícia oficial e beta.

Saída mínima:
- título final;
- resumo/excerpt;
- corpo do artigo;
- H2/H3 coerentes;
- observação de beta quando aplicável.

Regras obrigatórias:
- não inventar disponibilidade oficial para itens beta;
- não copiar trechos extensos da fonte;
- explicar impacto prático quando possível.

### Resultado esperado
O artigo gerado deve ser salvo como `draft`.

### Critérios de aceite
- conteúdo em PT-BR natural;
- sem aparência de tradução literal;
- `draft` criado com todos os metadados mínimos.

### Testes mínimos
- testes com provider mockado;
- validações de campos obrigatórios;
- golden set manual para revisão.

### Não fazer agora
- múltiplas versões do mesmo artigo;
- otimização SEO avançada por keyword research.

---

## Tarefa 15 — Persistir draft e referências de origem

### Objetivo
Garantir rastreabilidade editorial do artigo gerado.

### Implementar
Ao gerar draft:
- salvar artigo com status `draft`;
- associar `sourceItemId`;
- persistir referências de origem;
- persistir tipo editorial;
- persistir metadados SEO.

### Resultado esperado
Todo draft deve ser rastreável até a fonte original.

### Critérios de aceite
- draft persistido corretamente;
- referência de origem salva;
- draft sem campos essenciais ausentes.

### Testes mínimos
- integração validando criação completa do draft.

### Não fazer agora
- histórico completo de revisões editoriais.

---

## Tarefa 16 — Implementar publicação manual do draft

### Objetivo
Permitir transformação explícita de draft em artigo público.

### Implementar
- endpoint/comando para publicar draft;
- preenchimento de `publishedAt`;
- alteração de status para `published`;
- validação de integridade do draft antes de publicar.

### Resultado esperado
O sistema deve permitir separar geração e publicação.

### Critérios de aceite
- draft válido vira published;
- draft inválido não publica;
- publicação é idempotente.

### Testes mínimos
- integração cobrindo publicação;
- teste de dupla publicação.

### Não fazer agora
- fila de aprovação complexa;
- painel editorial completo.

---

## Tarefa 17 — Implementar orquestrador manual do pipeline real

### Objetivo
Criar o fluxo ponta a ponta acionável por comando/endpoint.

### Implementar
Fluxo mínimo:
1. buscar novidades por fonte;
2. normalizar itens;
3. deduplicar;
4. classificar;
5. gerar draft;
6. persistir;
7. opcionalmente publicar via ação separada.

### Resultado esperado
Um operador deve conseguir disparar o pipeline real manualmente.

### Critérios de aceite
- endpoint/comando executa sem intervenção adicional;
- logs por etapa;
- falha em um item não interrompe todos os demais.

### Testes mínimos
- integração do fluxo com provider mockado;
- smoke test manual.

### Não fazer agora
- agendamento automático contínuo.

---

## Tarefa 18 — Implementar modo demo `RunDemoPipeline`

### Objetivo
Garantir uma demonstração confiável mesmo sem posts novos durante o hackathon.

### Implementar
- endpoint/comando `RunDemoPipeline`;
- uso de fixtures reais em `samples`;
- o fluxo deve percorrer as mesmas etapas do pipeline real;
- flag `isDemoItem` para rastreabilidade;
- permitir reset ou reprocessamento previsível do cenário demo.

### Resultado esperado
Deve ser possível demonstrar descoberta, processamento, draft e publicação usando insumos controlados.

### Critérios de aceite
- cenário demo reproduzível;
- dados não precisam depender de publicação nova externa;
- pipeline real reutilizado, sem mockar o resultado final.

### Testes mínimos
- integração do modo demo;
- teste de reexecução idempotente.

### Não fazer agora
- dezenas de cenários demo;
- fixtures artificiais demais.

---

## Tarefa 19 — Expor endpoints mínimos para o front-end

### Objetivo
Disponibilizar ao Next.js somente o que é necessário para o portal público e para a demo operacional.

### Implementar
Endpoints mínimos sugeridos:
- `GET /articles/published`
- `GET /articles/{slug}`
- `GET /categories/{category}`
- `POST /pipeline/run`
- `POST /pipeline/run-demo`
- `POST /articles/{id}/publish`
- `GET /sources`

### Resultado esperado
O front deve conseguir listar e renderizar artigos publicados.

### Critérios de aceite
- contratos claros;
- DTOs enxutos;
- sem expor campos internos desnecessários.

### Testes mínimos
- integração dos endpoints.

### Não fazer agora
- autenticação completa;
- dashboard administrativo rico.

---

## Tarefa 20 — Inicializar o front-end Next.js

### Objetivo
Criar a base do portal público.

### Implementar
- projeto Next.js com TypeScript;
- App Router;
- estrutura base de layout;
- configuração de environment variables;
- página inicial mínima;
- client de API simples.

### Resultado esperado
O portal deve subir localmente e estar apto a consumir a API.

### Critérios de aceite
- front sobe sem erros;
- estrutura inicial limpa;
- integração via variável de ambiente com a API.

### Testes mínimos
- smoke test de build.

### Não fazer agora
- design system complexo;
- componentes em excesso.

---

## Tarefa 21 — Implementar página inicial do portal

### Objetivo
Exibir os artigos publicados de forma simples e clara.

### Implementar
A home deve ter:
- lista de artigos publicados;
- destaque visual mínimo;
- título do portal;
- cards responsivos;
- indicação visual de beta quando aplicável.

### Resultado esperado
O jurado deve chegar ao site e ver conteúdo real publicado.

### Critérios de aceite
- home carrega a partir da API;
- tratamento de loading/erro simples;
- responsividade básica.

### Testes mínimos
- teste de render básico;
- validação manual responsiva.

### Não fazer agora
- infinite scroll;
- filtros avançados.

---

## Tarefa 22 — Implementar página de artigo

### Objetivo
Renderizar o artigo publicado como unidade principal de SEO e navegação.

### Implementar
A página de artigo deve conter:
- título;
- resumo;
- corpo do artigo;
- data;
- categoria/tipo;
- badge beta quando aplicável;
- link/menção à fonte;
- heading hierarchy correta.

### Resultado esperado
Cada artigo deve ser uma página pública completa.

### Critérios de aceite
- rota por slug;
- renderização correta;
- estado beta destacado visualmente;
- HTML semântico.

### Testes mínimos
- render por slug;
- 404 simples para artigo inexistente.

### Não fazer agora
- comentários;
- related posts complexos.

---

## Tarefa 23 — Implementar página de categoria

### Objetivo
Atender o requisito de arquitetura temática mínima do portal.

### Implementar
Começar simples, com poucas categorias efetivas, por exemplo:
- `official`
- `beta`
- ou equivalente editorial mais amigável

A página deve listar artigos daquela categoria/tipo.

### Resultado esperado
O portal deve permitir navegação temática mínima.

### Critérios de aceite
- listagem funcional por categoria;
- navegação simples a partir da home/artigo.

### Testes mínimos
- render da listagem;
- categoria vazia tratada.

### Não fazer agora
- taxonomia muito profunda.

---

## Tarefa 24 — Implementar SEO técnico do front

### Objetivo
Atender aos requisitos obrigatórios de SEO do projeto.

### Implementar
Para páginas de artigo e categoria:
- title;
- meta description;
- canonical;
- Open Graph mínimo;
- JSON-LD `Article`/`NewsArticle`;
- sitemap;
- robots básico;
- URL limpa e descritiva.

### Resultado esperado
O portal deve estar tecnicamente apto para indexação e demonstração séria.

### Critérios de aceite
- metadata por rota;
- schema válido;
- sitemap gerado;
- URLs estáveis.

### Testes mínimos
- validação manual do HTML gerado;
- teste simples de presença de metadata quando viável.

### Não fazer agora
- News Sitemap separado, se isso ameaçar prazo;
- otimizações SEO fora do escopo mínimo.

---

## Tarefa 25 — Implementar botão ou rota operacional de demo/admin mínima

### Objetivo
Permitir disparo da demo sem necessidade de ferramentas externas.

### Implementar
Escolher uma das opções:
- rota simples protegida por segredo básico;
- script de linha de comando;
- pequena página interna de operador.

Para o hackathon, o mínimo aceitável é um mecanismo seguro e prático para:
- rodar pipeline real;
- rodar demo pipeline;
- publicar draft.

### Resultado esperado
Você deve conseguir operar o sistema durante a apresentação.

### Critérios de aceite
- execução prática e clara;
- sem necessidade de acessar banco manualmente;
- sem UI administrativa complexa.

### Testes mínimos
- smoke test manual.

### Não fazer agora
- painel admin completo.

---

## Tarefa 26 — Implementar logs e observabilidade mínima

### Objetivo
Dar visibilidade suficiente para debug durante o hackathon.

### Implementar
- logs estruturados por etapa do pipeline;
- correlation id simples por execução;
- mensagens de erro resumidas;
- endpoint health já existente;
- logs de publicação.

### Resultado esperado
Falhas devem ser diagnosticáveis rapidamente.

### Critérios de aceite
- logs distinguem descoberta, processamento, draft, publicação e falha;
- não expõem segredos.

### Testes mínimos
- validação manual em execução real.

### Não fazer agora
- tracing distribuído completo.

---

## Tarefa 27 — Implementar tratamento robusto de falhas do pipeline

### Objetivo
Evitar que um erro externo comprometa a demo inteira.

### Implementar
- `try/catch` por item processado;
- persistência de falha em `failed`;
- reprocessamento manual possível;
- timeouts de HTTP e IA;
- mensagens legíveis.

### Resultado esperado
O sistema deve falhar de forma controlada.

### Critérios de aceite
- falha em uma fonte/item não interrompe tudo;
- erro fica rastreável.

### Testes mínimos
- testes simulando erro da IA;
- testes simulando erro HTTP.

### Não fazer agora
- circuit breaker sofisticado.

---

## Tarefa 28 — Popular o sistema com artigos reais

### Objetivo
Garantir que o portal esteja pronto para jurados verem resultado real.

### Implementar
- rodar pipeline real sobre fontes definidas;
- gerar drafts válidos;
- publicar um conjunto mínimo de artigos reais;
- revisar rapidamente qualidade superficial dos textos.

### Resultado esperado
Ao final, o site deve conter conteúdo real derivado das fontes do escopo.

### Critérios de aceite
- pelo menos alguns artigos reais publicados;
- distinção correta entre oficial e beta;
- site navegável com conteúdo consistente.

### Testes mínimos
- validação manual do conteúdo publicado.

### Não fazer agora
- publicação em volume alto.

---

## Tarefa 29 — Deploy do banco no Render Postgres

### Objetivo
Provisionar persistência na stack final definida.

### Implementar
- criar banco no Render Postgres;
- configurar connection string no back-end;
- executar migrations;
- validar conectividade.

### Resultado esperado
Banco final do hackathon operacional.

### Critérios de aceite
- app conecta ao banco Render;
- migrations aplicadas;
- leitura/escrita funcionando.

### Testes mínimos
- smoke test de conexão e persistência.

### Não fazer agora
- migração para outro provider.

---

## Tarefa 30 — Deploy do back-end .NET no Render

### Objetivo
Publicar a API/pipeline na infraestrutura final.

### Implementar
- criar web service no Render;
- configurar variáveis de ambiente;
- configurar build/start corretos;
- validar `/health` em produção;
- validar conectividade com banco.

### Resultado esperado
A API deve estar acessível publicamente.

### Critérios de aceite
- deploy concluído;
- `/health` responde;
- pipeline pode ser acionado em produção.

### Testes mínimos
- smoke test pós-deploy.

### Observação importante
Aceitar o sleep do plano free. O sistema deve ser tolerante ao cold start.

---

## Tarefa 31 — Deploy do front-end Next.js na Vercel

### Objetivo
Publicar o portal acessível aos jurados.

### Implementar
- configurar projeto na Vercel;
- configurar variáveis de ambiente;
- apontar para API publicada;
- validar build e páginas;
- validar metadata e sitemap em produção.

### Resultado esperado
O portal público deve estar no ar com conteúdo publicado.

### Critérios de aceite
- home acessível publicamente;
- páginas de artigo acessíveis;
- consumo correto da API;
- SEO técnico mínimo visível no HTML final.

### Testes mínimos
- smoke test pós-deploy.

---

## Tarefa 32 — Validar fluxo real ponta a ponta em produção

### Objetivo
Comprovar que a implementação final funciona fora do ambiente local.

### Implementar
Executar e validar:
- descoberta de item real;
- criação de `SourceItem`;
- classificação;
- geração de draft;
- publicação;
- visualização no portal público.

### Resultado esperado
O sistema deve provar funcionamento real em ambiente publicado.

### Critérios de aceite
- artigo real publicado em produção;
- artigo visível no front;
- sem erro bloqueante.

### Testes mínimos
- checklist manual completo ponta a ponta.

---

## Tarefa 33 — Validar modo demo em produção

### Objetivo
Garantir plano de contingência para apresentação.

### Implementar
- executar `RunDemoPipeline` em produção ou ambiente final equivalente;
- validar geração e publicação a partir de fixture real controlada;
- documentar sequência operacional para a demo.

### Resultado esperado
Mesmo sem post novo externo, a demo precisa ser reproduzível.

### Critérios de aceite
- modo demo executa com sucesso;
- resultado final aparece no portal;
- roteiro operacional é claro.

### Testes mínimos
- execução manual completa do fluxo demo.

---

## Tarefa 34 — Revisão final de qualidade editorial, UX e confiabilidade

### Objetivo
Fazer o fechamento do projeto antes da apresentação.

### Implementar
Checklist final:
- revisar 3 a 5 artigos publicados;
- confirmar títulos, metas e badges beta;
- revisar links quebrados;
- revisar responsividade básica;
- revisar mensagens de erro críticas;
- revisar URLs.

### Resultado esperado
O sistema deve parecer coeso e confiável no nível de hackathon.

### Critérios de aceite
- sem falhas visuais graves;
- sem artigos beta apresentados como oficiais;
- navegação principal funcionando.

### Testes mínimos
- checklist manual final.

---

# 5. Ordem de priorização caso falte tempo

Se faltar tempo, manter nesta ordem de prioridade:

## Prioridade 1 — Obrigatório
- back-end funcionando
- banco funcionando
- ingestão mínima
- Gemini funcionando
- draft funcionando
- publicação funcionando
- home + página de artigo
- deploy publicado
- artigos reais publicados
- modo demo funcionando

## Prioridade 2 — Muito desejável
- página de categoria
- sitemap
- JSON-LD
- logs melhores
- UX melhor na home

## Prioridade 3 — Cortável
- painel admin elaborado
- taxonomia aprofundada
- scraping avançado
- automação contínua
- features cosméticas

---

# 6. Definição de pronto por tarefa

Uma tarefa só pode ser considerada pronta quando:

1. objetivo implementado sem quebrar contratos existentes;
2. código compila/builda;
3. testes aplicáveis passam;
4. há validação manual descrita;
5. logs/erros são compreensíveis;
6. arquivos alterados são listados;
7. pendências são explicitadas.

---

# 7. Antiobjetivos

Para este hackathon, o agente **não deve** tentar implementar espontaneamente:
- busca semântica avançada;
- clusterização por embeddings;
- múltiplos provedores de IA;
- painel CMS completo;
- autenticação complexa;
- sistema editorial enterprise;
- filas distribuídas sofisticadas;
- microserviços;
- otimizações prematuras;
- refactors amplos fora da tarefa atual.

---

# 8. Forma recomendada de solicitar execução ao agente

Modelo recomendado por tarefa:

```text
Implemente apenas a Tarefa XX do plano de implementação.
Respeite a arquitetura e os limites de escopo definidos.
Não antecipe tarefas futuras.
Ao final, entregue:
1. resumo do que foi implementado;
2. arquivos alterados;
3. testes criados/executados;
4. como validar manualmente;
5. riscos ou pendências.
```

---

# 9. Resultado final esperado do projeto

Ao final da execução deste plano, o projeto deve permitir:
- monitorar fontes definidas;
- encontrar conteúdo novo;
- tratar esse conteúdo;
- gerar artigo em PT-BR com IA;
- salvar como draft;
- publicar o artigo;
- exibir o conteúdo em um portal público com SEO técnico mínimo;
- diferenciar claramente notícia oficial de notícia beta;
- demonstrar o fluxo ponta a ponta mesmo sem novos posts externos.
