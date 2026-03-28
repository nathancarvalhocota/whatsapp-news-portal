# API .NET — WhatsApp News Portal

Back-end do portal de notícias sobre WhatsApp.

## Responsabilidades

- API REST para o front-end
- Pipeline editorial (ingestão, classificação, geração de artigos via Gemini)
- Persistência com EF Core + PostgreSQL
- Modo demo para apresentação do hackathon

## Stack

- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL (Render Postgres)
- Gemini API (Flash-Lite para classificação, Flash para geração)

## Como rodar

```bash
# Copiar variáveis de ambiente
cp .env.example .env

# Rodar a API
dotnet run
```

A API sobe por padrão em `http://localhost:5000`.
