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
