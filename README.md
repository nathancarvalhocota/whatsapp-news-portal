# Portal de Notícias sobre WhatsApp

Portal de conteúdo em português do Brasil, 100% dedicado ao ecossistema WhatsApp. Monitora fontes oficiais e especializadas, gera artigos originais com IA e publica automaticamente em um site otimizado para SEO.

## O que faz

1. **Monitora** fontes oficiais do WhatsApp e WABetaInfo
2. **Detecta** novos conteúdos publicados
3. **Gera** artigos originais em PT-BR com apoio de IA (Gemini)
4. **Publica** no portal com SEO técnico e distinção clara entre notícias oficiais e recursos beta
5. **Demonstra** o fluxo ponta a ponta via modo demo

## Stack

| Camada | Tecnologia |
|--------|-----------|
| Front-end | Next.js, React, TypeScript, Tailwind CSS |
| Back-end | .NET / ASP.NET Core Web API |
| Banco de dados | PostgreSQL |
| IA | Gemini 2.5 Flash-Lite (classificação) + Gemini 2.5 Flash (geração) |
| Deploy | Vercel (front) · Render (API + PostgreSQL) |

## Estrutura do repositório

```
/apps
  /web-next        → Front-end público (Next.js)
  /api-dotnet      → API e pipeline editorial (.NET)
/docs              → Documentação do projeto
/samples           → Fixtures para modo demo
/scripts           → Scripts de apoio
```

## Fontes monitoradas

- Blog oficial do WhatsApp
- Blog oficial do WhatsApp Business
- WABetaInfo *(tratado como beta/em testes)*

## Desenvolvimento local

### Pré-requisitos

- Node.js 18+
- .NET 8 SDK
- PostgreSQL

### Back-end

```bash
cd apps/api-dotnet
cp .env.example .env          # configurar variáveis
dotnet restore
dotnet run
```

### Front-end

```bash
cd apps/web-next
cp .env.example .env.local    # configurar variáveis
npm install
npm run dev
```

## Documentação

A documentação completa do projeto está em `/docs/`:

- [Contexto Geral](docs/01-contexto-geral-do-projeto.md)
- [Regras de Arquitetura](docs/02-regras-de-arquitetura-e-desenvolvimento-assistido-por-ia.md)
- [Objetivos de Entrega](docs/03-objetivos-de-entrega.md)
- [Plano de Implementação](docs/04-plano-de-implementacao.md)
- [Progresso](docs/PROGRESS.md)

## Nota para os jurados

O portal está no ar, mas **a página inicial exibe APENAS DOIS ARTIGOS** — isso é intencional. 
Mantive dessa forma para que fosse possível acompanhar o fluxo ponta a ponta sem precisar esperar uma nova publicação surgir organicamente nas fontes monitoradas.

Para ver o pipeline funcionando em tempo real, siga os passos abaixo:

1. Acesse a página de **Admin** (link no rodapé do site, ou `/admin` direto na URL)
2. Faça login com a senha: `hackathon`
3. Altere o intervalo de monitoramento para **1 minuto**
4. Aguarde o próximo ciclo — o sistema irá buscar as fontes, detectar novos conteúdos, gerar os artigos com IA e publicá-los automaticamente no portal

### Alternativa: processar uma URL diretamente

Na mesma página de Admin há uma seção **"Demo Pipeline"** que permite forçar o processamento de um artigo específico. Basta colar a URL completa de um post de uma das fontes monitoradas e clicar em **"Rodar Demo"** — o sistema irá buscar o conteúdo, gerar o artigo com IA e deixá-lo disponível para publicação imediata.

> **Atenção:** a URL precisa pertencer a uma das fontes aceitas. URLs de outros domínios são rejeitadas.
>
> Fontes válidas:
> - `blog.whatsapp.com`
> - `business.whatsapp.com`
> - `wabetainfo.com`

---

Projeto desenvolvido para hackathon — Umbler.
