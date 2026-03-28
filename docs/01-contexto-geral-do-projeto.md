# Contexto Geral do Projeto

## 1. Visão geral

### Nome do projeto
Portal de Notícias sobre WhatsApp

### Propósito
Criar um portal de conteúdo em português, 100% dedicado ao ecossistema WhatsApp, com foco em aquisição orgânica via SEO e geração automatizada de artigos a partir do monitoramento contínuo de fontes relevantes.

O portal deve transformar novidades, atualizações, mudanças de política, recursos do WhatsApp Business, documentação da API oficial e conteúdos beta em artigos originais, contextualizados para o público brasileiro e prontos para publicação.

### Objetivo de negócio
Posicionar a marca Umbler diante de empreendedores, profissionais e empresas que utilizam WhatsApp como ferramenta de negócio, capturando tráfego orgânico de alto volume a partir de buscas relacionadas a:

- novidades do WhatsApp
- WhatsApp Business
- API oficial do WhatsApp
- políticas e regras de uso
- segurança e privacidade
- recursos beta e novos recursos
- boas práticas de uso comercial do WhatsApp

### Objetivo do hackathon
Entregar um produto mínimo funcional, publicamente acessível, demonstrável ponta a ponta, com posts reais já publicados a partir das fontes definidas e com capacidade de simular a descoberta de um novo post caso nenhuma novidade real seja publicada durante a janela do hackathon.

---

## 2. Escopo funcional do produto

O sistema deve:

1. Monitorar fontes definidas.
2. Detectar conteúdos novos.
3. Armazenar itens descobertos no banco.
4. Classificar o item quanto à origem e natureza editorial.
5. Preparar o item para a esteira de geração.
6. Gerar conteúdo original em PT-BR com apoio de IA.
7. Criar metadados SEO.
8. Persistir artigos em estados intermediários e finais.
9. Publicar artigos no portal.
10. Exibir os artigos publicados em um site público otimizado para SEO.
11. Permitir demonstração manual do fluxo ponta a ponta por meio de modo demo.

---

## 3. Fontes no escopo

### Fontes oficiais e primárias
As fontes primárias e oficiais do projeto são:

- Blog oficial do WhatsApp
- Blog oficial do WhatsApp Business
- Documentação oficial da API / WhatsApp Business Platform

### Fonte secundária especializada
Também faz parte do escopo:

- WABetaInfo

### Regra editorial obrigatória por tipo de fonte
- Conteúdo oriundo de **fontes oficiais** pode ser tratado como novidade oficial.
- Conteúdo oriundo do **WABetaInfo** deve sempre ser tratado como **beta / em testes / ainda não oficialmente lançado**.
- O sistema não deve apresentar conteúdo do WABetaInfo como anúncio oficial, lançamento definitivo ou disponibilidade geral.

---

## 4. Requisitos funcionais

### RF-01 — Monitoramento de fontes
O sistema deve ser capaz de consultar periodicamente as fontes definidas e identificar novos conteúdos publicados.

### RF-02 — Descoberta de novos itens
O sistema deve registrar cada item descoberto com informações mínimas como:

- URL
- título original
- data de publicação, quando disponível
- fonte
- conteúdo bruto ou referência suficiente para posterior processamento

### RF-03 — Tratamento inicial do item
Cada item descoberto deve passar por uma etapa de tratamento inicial para:

- validar se é relevante para o portal
- normalizar metadados
- remover duplicidades básicas
- preparar o item para a esteira de geração

### RF-04 — Estados do pipeline
Os itens e artigos devem trafegar por estados mínimos de processamento:

- `discovered`
- `processing`
- `draft`
- `published`
- `failed`

### RF-05 — Regra de entrada na esteira
Itens em `processing` ou `draft` devem representar conteúdos válidos para a esteira, isto é, conteúdos que podem realmente virar artigo, mas que ainda não estão prontos para publicação pública.

### RF-06 — Geração editorial com IA
O sistema deve gerar artigos originais em português do Brasil a partir dos conteúdos monitorados.

O conteúdo gerado deve:

- ser original
- não ser mera tradução
- não ser mera paráfrase do original
- contextualizar para o público brasileiro
- agregar valor editorial
- explicar impacto prático quando relevante

### RF-07 — Marcação editorial de beta
Artigos gerados a partir do WABetaInfo devem conter sinalização clara de que tratam de recurso beta, em testes ou ainda não lançado oficialmente.

### RF-08 — Metadados SEO
Cada artigo deve possuir ao menos:

- URL limpa e descritiva
- título otimizado
- meta description
- heading hierarchy adequada
- marcação estruturada compatível com `Article` ou `NewsArticle`

### RF-09 — Publicação
O sistema deve ser capaz de publicar artigos finalizados no portal público.

### RF-10 — Exibição pública
O portal deve exibir os artigos publicados com layout responsivo e estrutura orientada a SEO.

### RF-11 — Modo demo
O sistema deve disponibilizar uma ação manual chamada `RunDemoPipeline` ou equivalente, capaz de reexecutar o fluxo real ponta a ponta usando insumos controlados, permitindo demonstrar:

- descoberta simulada de novo item
- tratamento
- geração
- persistência
- publicação

### RF-12 — Conteúdo real já publicado
Ao final do hackathon, o portal deve conter artigos já publicados com base em conteúdos reais obtidos das fontes definidas.

---

## 5. Requisitos não funcionais

### RNF-01 — Prioridade do projeto
A prioridade máxima é entregar um fluxo funcional ponta a ponta, estável, demonstrável e publicamente acessível.

### RNF-02 — Simplicidade arquitetural
O projeto deve privilegiar simplicidade e confiabilidade sobre complexidade desnecessária.

### RNF-03 — Escopo mínimo funcional
É preferível entregar menos funcionalidades, desde que reais, testáveis e demonstráveis, do que propor componentes sofisticados que não fiquem prontos.

### RNF-04 — Performance mínima do portal
As páginas públicas devem possuir carregamento rápido, estrutura compatível com SEO e boa experiência em dispositivos móveis.

### RNF-05 — Tolerância ao ambiente gratuito
A arquitetura deve considerar as limitações da stack gratuita de deploy, especialmente o comportamento de sleep do back-end hospedado no Render.

### RNF-06 — Robustez mínima operacional
O sistema deve ter tratamento mínimo de erro, logs legíveis e comportamento previsível em falhas.

### RNF-07 — Publicação pública real
Ao final da entrega, o projeto deve estar efetivamente publicado em ambiente acessível pela internet, sem depender apenas de execução local.

---

## 6. Regras editoriais essenciais

### 6.1 Originalidade
O sistema não deve apenas reproduzir, traduzir ou resumir literalmente o conteúdo da fonte.

### 6.2 Contextualização
O texto final deve contextualizar a informação para o público brasileiro.

### 6.3 Valor editorial
Sempre que aplicável, o artigo deve explicar:

- o que mudou
- quem é impactado
- por que isso importa
- impacto prático para uso comum, negócio ou integração técnica

### 6.4 Diferenciação entre oficial e beta
O sistema deve distinguir claramente:

- conteúdo oficial
- conteúdo beta / em testes

### 6.5 Segurança factual
O sistema não deve afirmar disponibilidade oficial quando a fonte indicar apenas testes, desenvolvimento ou beta.

---

## 7. Fluxo macro esperado

O fluxo macro esperado do sistema é:

1. Monitorar fonte
2. Encontrar item novo
3. Registrar item no banco
4. Validar relevância e natureza editorial
5. Colocar item em processamento
6. Executar tratamento com IA
7. Gerar draft de artigo
8. Gerar metadados SEO
9. Persistir artigo
10. Publicar artigo
11. Exibir no portal

Além disso, deve existir um fluxo paralelo de demonstração:

1. Acionar `RunDemoPipeline`
2. Carregar fixture ou insumo real controlado
3. Processar pelo mesmo pipeline real
4. Gerar draft
5. Finalizar publicação
6. Tornar o resultado visível no portal

---

## 8. Stack de código

### Front-end
- Next.js
- React
- TypeScript
- Tailwind CSS

### Back-end
- .NET
- ASP.NET Core Web API

### Banco de dados
- PostgreSQL
- Render Postgres

### IA para processamento de conteúdo
- `gemini-2.5-flash-lite` para:
  - classificação
  - limpeza
  - sumarização curta
  - geração de slug
  - meta description
  - tags e metadados auxiliares
- `gemini-2.5-flash` para:
  - geração do artigo final em PT-BR

### Observação sobre fallback
Não haverá fallback para outro provedor de IA neste estágio do hackathon, a fim de preservar simplicidade e foco na implementação do mínimo funcional.

---

## 9. Stack de deploy

### Front-end público
- Vercel Hobby

### Back-end
- Render Web Service

### Banco
- Render Postgres

### Observação operacional importante
O back-end hospedado no Render poderá entrar em sleep em períodos de inatividade. A arquitetura e a demonstração do projeto devem tolerar essa característica.

---

## 10. Restrições e prioridades do hackathon

### Restrições práticas
- desenvolvimento realizado por uma única pessoa com apoio intensivo de IA
- tempo restante limitado
- uso apenas de serviços gratuitos ou compatíveis com o contexto do hackathon
- não utilizar API paga de LLM para a implementação principal

### Prioridades absolutas
1. fluxo ponta a ponta funcional
2. publicação real do sistema
3. posts reais publicados no portal
4. modo demo confiável
5. SEO técnico mínimo correto
6. clareza editorial entre conteúdo oficial e beta

### Prioridades secundárias
- refinamento visual extra
- automações sofisticadas
- painéis administrativos complexos
- busca avançada
- arquitetura altamente escalável

---

## 11. Fora de escopo para esta fase

Nesta etapa inicial, ficam explicitamente fora de escopo, salvo necessidade extrema:

- painel administrativo complexo
- autenticação completa
- busca semântica avançada
- múltiplos provedores de IA
- clusterização sofisticada multi-evento
- sistema editorial completo com múltiplos papéis
- infraestrutura complexa orientada a escala
- automações que não impactem diretamente a demonstração final

---

## 12. Definição de sucesso desta etapa

Esta etapa será considerada bem-sucedida quando houver:

- portal publicado e acessível
- artigos reais publicados a partir das fontes definidas
- fluxo ponta a ponta funcional
- diferenciação correta entre conteúdo oficial e beta
- geração de conteúdo original em PT-BR
- capacidade de demonstrar o pipeline por meio do modo demo
- base técnica suficiente para evolução posterior
