# CLAUDE.md

Portal de Notícias sobre WhatsApp — hackathon Umbler. Geração automatizada de artigos em PT-BR via Gemini a partir de fontes monitoradas do ecossistema WhatsApp.

## Stack (fixa, não alterar)
- **Front:** Next.js App Router + TypeScript + Tailwind
- **Back:** ASP.NET Core + EF Core + PostgreSQL
- **IA:** gemini-2.5-flash-lite (metadados) + gemini-2.5-flash (artigos). Sem fallback.
- **Deploy:** Vercel (front) + Render (back + banco, tolerar cold start)

## Convenções
- **Pipeline:** discovered → processing → draft → published | failed
- **Fontes:** `official` (WhatsApp Blog, Business Blog, docs API) | `beta_specialized` (WABetaInfo)
- **Artigos:** `official_news` | `beta_news` (WABetaInfo = SEMPRE beta/em testes, NUNCA oficial)
- **SEO por artigo:** URL limpa, meta tags, JSON-LD (schema.org/Article), heading hierarchy, sitemap

## Regras de trabalho
1. UMA tarefa por vez, fornecida no prompt. Não ler /docs/04 inteiro.
2. Não adicionar libs, complexidade ou features fora do escopo da tarefa.
3. Ao concluir: código compila, testes passam, atualizar /docs/PROGRESS.md.

## Docs de referência (ler apenas quando solicitado)
- `/docs/01-contexto-geral-do-projeto.md` — requisitos funcionais
- `/docs/02-regras-de-arquitetura.md` — qualidade, testes, aceite
- `/docs/03-objetivos-de-entrega.md` — critérios de sucesso
- `/docs/04-plano-de-implementacao.md` — tarefas sequenciais