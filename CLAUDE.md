# CLAUDE.md — Regras e Contexto do Projeto

## Projeto

Portal de Notícias sobre WhatsApp. Portal de conteúdo em PT-BR dedicado ao ecossistema WhatsApp, com geração automatizada de artigos via IA a partir de fontes monitoradas. Objetivo: aquisição orgânica via SEO para a marca Umbler.

## Fonte de verdade

Toda decisão deve ser baseada nos documentos em `/docs/`:
- `/docs/01-contexto-geral-do-projeto.md` — escopo, requisitos, stack, restrições
- `/docs/02-regras-de-arquitetura-e-desenvolvimento-assistido-por-ia.md` — arquitetura, qualidade, testes, aceite
- `/docs/03-objetivos-de-entrega.md` — critérios de sucesso, entrega mínima, checklist
- `/docs/04-plano-de-implementacao.md` — 34 tarefas sequenciais com critérios de aceite

## Stack — NÃO ALTERAR

### Front-end
- Next.js (App Router)
- React
- TypeScript
- Tailwind CSS

### Back-end
- .NET / ASP.NET Core Web API
- EF Core + PostgreSQL

### Banco de dados
- PostgreSQL (Render Postgres)

### IA
- `gemini-2.5-flash-lite` — classificação, limpeza, sumarização, slug, meta description, tags
- `gemini-2.5-flash` — geração do artigo final em PT-BR
- Sem fallback para outro provedor. Sem múltiplos provedores.

### Deploy
- Front-end: Vercel Hobby
- Back-end: Render Web Service (tolerar sleep/cold start)
- Banco: Render Postgres

A stack acima é fixa. Não substituir, não adicionar frameworks/libs sem justificativa explícita e aprovação.

## Estrutura do monorepo

```
/
  /apps
    /web-next         — front-end público (Next.js)
    /api-dotnet       — API, pipeline, integrações (.NET)
  /docs               — documentos do projeto (fonte de verdade)
  /samples            — fixtures HTML/JSON para modo demo
  /scripts            — apoio local e deploy
```

## Convenções obrigatórias

### Status do pipeline
- `discovered` — item recém-detectado
- `processing` — item em tratamento/geração
- `draft` — conteúdo gerado, não publicado
- `published` — conteúdo público no portal
- `failed` — falha, exige análise

### Tipos de fonte
- `official` — fontes oficiais (WhatsApp Blog, WhatsApp Business Blog, documentação API)
- `beta_specialized` — WABetaInfo

### Tipos editoriais
- `official_news` — artigo de fonte oficial
- `beta_news` — artigo de fonte beta/especializada

### Organização do back-end
Módulos mínimos:
- `Sources` — fontes monitoradas
- `Ingestion` — ingestão de conteúdo
- `ContentProcessing` — normalização e classificação
- `AiGeneration` — integração com Gemini
- `Articles` — artigos e publicação
- `Demo` — modo demo
- `Common` — utilitários compartilhados

Separação mínima: Domain, Application, Infrastructure, Api (ou pastas por módulo com interfaces claras).

### Regras do front-end
- App Router obrigatório
- Páginas públicas com SSR/SEO
- Não depender do back-end em tempo real para navegação básica quando evitável
- Funcionar mesmo com back-end frio (cold start Render)
- Design responsivo e rápido

## Regras de trabalho para o agente

1. Executar UMA tarefa por vez, conforme `/docs/04-plano-de-implementacao.md`.
2. SEMPRE consultar `/docs/04-plano-de-implementacao.md` antes de iniciar qualquer tarefa.
3. SEMPRE atualizar `/docs/PROGRESS.md` ao concluir cada tarefa.
4. Não antecipar tarefas futuras sem necessidade explícita.
5. Não redesenhar a arquitetura definida nos docs.
6. Não adicionar bibliotecas sem justificativa aprovada.
7. Não implementar funcionalidades fora do escopo da tarefa atual.
8. Não trocar stack definida.
9. Não reestruturar o projeto inteiro sem necessidade.
10. Não remover testes sem substituição adequada.
11. Cada tarefa deve terminar com: código compilando, testes passando, validação manual descrita, arquivos alterados listados, riscos/pendências explicitados.
12. Quando a tarefa disser "não implementar agora", respeitar rigidamente.

## Regras editoriais obrigatórias

### Originalidade
O conteúdo gerado NÃO pode ser tradução, cópia ou paráfrase rasa. Deve ser reescrito, contextualizado para o público brasileiro, com valor editorial próprio.

### Contextualização PT-BR
Todo artigo deve explicar: o que mudou, quem é impactado, por que importa, impacto prático para uso comum/negócio/integração técnica.

### Tratamento WABetaInfo
- Conteúdo do WABetaInfo SEMPRE tratado como beta/em testes/não oficialmente lançado.
- NUNCA apresentar como anúncio oficial, lançamento definitivo ou disponibilidade geral.
- Artigos devem conter sinalização clara do estágio não oficial.

### Segurança factual
Não afirmar disponibilidade oficial quando a fonte indicar apenas testes/desenvolvimento/beta.
Não inventar dados, datas ou funcionalidades.

## Antiobjetivos — NÃO implementar espontaneamente

- Busca semântica avançada
- Clusterização por embeddings
- Múltiplos provedores de IA
- Painel CMS completo
- Autenticação complexa
- Sistema editorial enterprise
- Filas distribuídas sofisticadas
- Microserviços
- Otimizações prematuras
- Refactors amplos fora da tarefa atual
- Automações que não impactem a demo final
- Infraestrutura enterprise/alta escalabilidade

## Critério supremo de decisão

Em caso de dúvida entre abordagens, vence a que melhor satisfaz simultaneamente:
1. Maior chance de funcionar de verdade
2. Menor risco para a demo final
3. Menor complexidade acidental
4. Maior aderência aos requisitos obrigatórios do projeto

## Prioridades absolutas

1. Fluxo ponta a ponta funcional
2. Publicação real do sistema
3. Posts reais publicados no portal
4. Modo demo confiável
5. SEO técnico mínimo correto
6. Clareza editorial entre conteúdo oficial e beta

## Definição de pronto por tarefa

1. Objetivo implementado sem quebrar contratos existentes
2. Código compila/builda
3. Testes aplicáveis passam
4. Validação manual descrita
5. Logs/erros compreensíveis
6. Arquivos alterados listados
7. Pendências explicitadas
8. `/docs/PROGRESS.md` atualizado
