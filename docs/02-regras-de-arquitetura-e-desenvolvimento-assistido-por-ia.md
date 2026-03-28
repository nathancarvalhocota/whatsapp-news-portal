# Regras de Arquitetura e Desenvolvimento Assistido por IA

## Objetivo deste documento

Este documento define as regras obrigatórias de arquitetura, desenvolvimento assistido por IA, qualidade, testes, aceite e supervisão para o projeto **Portal de Notícias sobre WhatsApp**.

Ele deve ser tratado como documento normativo para agentes de codificação e para qualquer atividade de implementação, revisão, refatoração ou deploy.

O objetivo é garantir que o desenvolvimento assistido por IA produza um sistema:
- seguro;
- consistente;
- escalável o suficiente para o escopo do hackathon;
- tecnicamente sólido;
- aderente aos requisitos do projeto;
- demonstrável com funcionamento real em produção.

---

## Princípios gerais de arquitetura

### 1. Prioridade máxima

A prioridade máxima é:

**entregar um fluxo funcional ponta a ponta, estável, compreensível e demonstrável, acima de sofisticação arquitetural desnecessária.**

Toda decisão técnica deve favorecer:
- clareza de implementação;
- previsibilidade do comportamento;
- facilidade de validação;
- baixo risco de quebra na demo;
- manutenção simples.

### 2. Arquitetura orientada ao fluxo principal

O sistema deve ser desenhado em torno do fluxo principal:

1. descobrir novo conteúdo em fonte monitorada;
2. persistir item descoberto;
3. normalizar e classificar;
4. validar elegibilidade para a esteira;
5. gerar draft com IA;
6. persistir artigo gerado;
7. publicar artigo;
8. disponibilizar página pública SEO-ready no portal.

Tudo que não contribui diretamente para este fluxo deve ser considerado secundário.

### 3. Separação clara de responsabilidades

A arquitetura deve evitar serviços ou módulos “God Object”.

O sistema deve manter responsabilidades separadas, ainda que no mesmo repositório ou serviço:
- ingestão/coleta;
- normalização e classificação;
- geração de conteúdo;
- persistência;
- publicação;
- renderização pública no front-end.

### 4. Simplicidade acima de generalização prematura

Não deve haver abstração excessiva, engines genéricas demais ou mecanismos altamente configuráveis se isso não for necessário para o hackathon.

Evitar:
- sistemas de plugin complexos;
- múltiplos provedores de IA no primeiro momento;
- múltiplos bancos de dados;
- múltiplas filas e workers independentes sem necessidade;
- arquitetura distribuída além do necessário.

---

## Regras de arquitetura da aplicação

## Front-end

### Stack obrigatória
- Next.js
- App Router
- TypeScript

### Responsabilidades do front-end
- renderizar páginas públicas do portal;
- exibir artigos publicados;
- exibir páginas de categoria;
- garantir SEO técnico por página;
- disponibilizar sitemap e metadados necessários;
- manter design responsivo e rápido.

### Regras obrigatórias
- o front-end não deve depender do back-end em tempo real para navegação pública básica sempre que isso puder ser evitado;
- páginas de artigo devem priorizar renderização adequada para SEO;
- a experiência pública deve continuar utilizável mesmo que o back-end esteja frio (cold start) no Render.

## Back-end

### Stack obrigatória
- .NET / ASP.NET Core
- PostgreSQL no Render

### Responsabilidades do back-end
- monitorar fontes;
- descobrir novos itens;
- persistir itens descobertos;
- aplicar normalização e classificação;
- executar geração com IA;
- salvar drafts e artigos publicados;
- disponibilizar endpoints necessários para o front e para operação;
- oferecer endpoint/ação para modo demo.

### Regras obrigatórias
- o back-end deve aceitar o comportamento de sleep do Render sem comprometer a demonstração do produto;
- o fluxo deve poder ser acionado manualmente;
- operações críticas devem ser idempotentes;
- erros devem ser persistidos ou logados com clareza.

## Banco de dados

### Regras obrigatórias
- PostgreSQL é o banco principal e único do MVP;
- o modelo deve ser relacional;
- o schema deve privilegiar rastreabilidade e clareza;
- evitar modelagem excessivamente genérica.

### O banco deve suportar no mínimo
- fontes;
- itens descobertos;
- artigos;
- categorias/tags básicas, se implementadas;
- status da esteira;
- metadados de publicação;
- logs ou registros mínimos de falha/processamento.

---

## Estados mínimos da esteira

O sistema deve usar, no mínimo, estes estados:
- `discovered`
- `processing`
- `draft`
- `published`
- `failed`

### Semântica obrigatória
- `discovered`: item recém-detectado a partir de uma fonte;
- `processing`: item elegível e em tratamento/geração;
- `draft`: conteúdo gerado e persistido, ainda não publicado;
- `published`: conteúdo disponível publicamente no portal;
- `failed`: item com falha que exige reprocessamento ou análise.

---

## Regras editoriais obrigatórias para arquitetura

### 1. Fontes oficiais vs fonte secundária especializada

O sistema deve distinguir explicitamente:
- **fontes oficiais**;
- **fonte secundária especializada**.

### 2. Tratamento obrigatório do WABetaInfo

Conteúdos do **WABetaInfo**:
- não devem ser tratados como anúncio oficial;
- devem ser marcados como **beta / em testes / ainda não oficialmente lançado**;
- devem deixar claro no artigo o estágio não oficial do recurso.

### 3. Tratamento obrigatório do texto gerado

O pipeline não pode gerar apenas tradução, cópia ou paráfrase rasa.

O conteúdo gerado deve:
- reescrever em PT-BR;
- contextualizar para o público brasileiro;
- explicar impacto prático quando relevante;
- manter fidelidade factual à fonte;
- evitar invenção de dados, datas ou funcionalidades.

---

## Regras para desenvolvimento assistido por IA

## Princípio central

A IA é uma ferramenta de implementação assistida, não uma autoridade autônoma de arquitetura, escopo ou aceite.

A IA pode:
- propor;
- implementar;
- refatorar;
- gerar testes;
- revisar.

A IA não pode:
- redefinir requisitos sem autorização explícita;
- alterar contratos públicos sem justificativa;
- adicionar complexidade fora do escopo;
- considerar uma entrega “pronta” sem evidência objetiva de funcionamento.

## Modelo de trabalho obrigatório

Cada solicitação para agentes de codificação deve incluir, sempre que possível:
- objetivo claro;
- contexto suficiente;
- restrições técnicas;
- arquivos ou módulos permitidos;
- comportamento esperado;
- testes esperados;
- critérios de aceite.

## Proibições para agentes de IA

Agentes não devem:
- reestruturar o projeto inteiro sem necessidade;
- trocar stack definida;
- introduzir bibliotecas não justificadas;
- implementar funcionalidades não pedidas;
- alterar naming e padrões arbitrariamente;
- remover testes sem substituição adequada;
- pular validação local/automática quando aplicável.

---

## Estratégia de desenvolvimento recomendada

## Abordagem obrigatória

O desenvolvimento deve seguir um modelo híbrido:
- **spec-driven development** no topo;
- **TDD seletivo** em regras determinísticas;
- **testes de integração e E2E** para fluxo real;
- **evals/validação por regras** nos trechos que dependem de IA.

## O que isso significa na prática

### Spec-driven
Usar especificação clara para:
- comportamento do pipeline;
- estados da esteira;
- contratos entre módulos;
- regras editoriais;
- SEO técnico;
- deploy e comportamento do modo demo.

### TDD seletivo
Aplicar TDD forte em:
- parsing e normalização;
- slug generation;
- canonicalização básica;
- validação de status;
- regras de elegibilidade;
- regras de marcação beta/oficial;
- serialização/DTOs;
- helpers de SEO quando determinísticos.

### Testes de integração
Obrigatórios para:
- API + banco;
- ingestão real ou semi-real;
- persistência de itens;
- geração e persistência de draft;
- publicação de artigo;
- modo demo.

### Testes E2E
Obrigatórios para:
- home;
- página de artigo;
- página de categoria, se existir;
- presença de metadados principais;
- página publicada acessível após pipeline.

### Validação de IA
Deve haver verificação por regras, ao menos mínima, para confirmar que:
- o texto não está vazio;
- o texto não é mera cópia óbvia da fonte;
- o artigo possui os campos esperados;
- a marcação beta/oficial foi respeitada;
- o idioma final é PT-BR;
- slug, título e meta foram gerados.

---

## Regras de qualidade de código

### Regras obrigatórias
- código deve ser legível e coeso;
- nomes devem ser explícitos;
- funções/métodos devem ter responsabilidade clara;
- evitar acoplamento desnecessário;
- evitar duplicação evidente;
- preferir composição simples a abstrações excessivas.

### Boas práticas obrigatórias
- DTOs explícitos;
- validação de entrada;
- tratamento de erro consistente;
- logging estruturado ou, no mínimo, claro e rastreável;
- configuração via variáveis de ambiente;
- segredos nunca hardcoded.

### Proibições
- código morto desnecessário;
- comentários redundantes para explicar código ruim;
- dependências não utilizadas;
- hacks sem registro claro da limitação;
- SQL ou HTML montado de forma insegura sem necessidade.

---

## Contratos e interfaces

### Regras obrigatórias
Toda fronteira entre partes relevantes do sistema deve possuir contrato claro.

Isso inclui, quando aplicável:
- payload de item descoberto;
- payload de item processado;
- payload do draft gerado;
- resposta da IA estruturada;
- contrato de artigo publicado;
- contrato da API consumida pelo front-end.

### Recomendação obrigatória
Sempre que viável, respostas da IA devem ser estruturadas em formato previsível, evitando texto livre como único mecanismo de integração.

---

## Supervisão humana e revisão

Mesmo com desenvolvimento intensamente assistido por IA, a supervisão humana continua obrigatória.

### A supervisão deve garantir
- aderência ao escopo real do hackathon;
- coerência arquitetural;
- qualidade mínima do código;
- consistência entre módulos;
- conformidade com critérios de aceite;
- ausência de desvio para complexidade supérflua.

### Toda entrega relevante deve ser revisada quanto a
- requisito atendido;
- impacto em arquitetura;
- testes adicionados/atualizados;
- riscos criados;
- comportamento em produção;
- impacto na demo.

---

## Critérios gerais de aceite técnico

Uma entrega só pode ser considerada aceita quando:
- atende ao comportamento esperado;
- respeita a stack definida;
- não viola regras editoriais;
- não quebra contratos existentes sem justificativa explícita;
- possui teste compatível com o nível de risco;
- pode ser validada de forma objetiva;
- não adiciona complexidade desnecessária;
- contribui para o fluxo principal do produto.

---

## Critérios mínimos para “pronto”

Um item implementado só pode ser considerado “pronto” quando:
- código compila/builda corretamente;
- comportamento principal funciona;
- testes relevantes passam;
- logs de erro e caminhos principais são verificáveis;
- não há bloqueio óbvio para deploy;
- o resultado é compatível com a demo final.

---

## Regras de segurança e produção

### Segurança mínima obrigatória
- secrets apenas por variáveis de ambiente;
- nenhuma credencial em repositório;
- validação básica de entradas;
- sanitização quando necessário;
- cuidado com renderização de HTML ou conteúdo externo.

### Produção mínima obrigatória
- aplicação publicada em ambiente acessível publicamente;
- banco conectado corretamente;
- pipeline funcional para pelo menos alguns artigos reais;
- páginas públicas navegáveis;
- erro não deve derrubar a aplicação inteira;
- deve existir ao menos um endpoint de health ou verificação simples.

---

## Regras específicas do modo demo

O sistema deve possuir uma forma controlada de demonstrar o fluxo ponta a ponta mesmo sem a existência de um novo post real durante o hackathon.

### Regras obrigatórias
- o modo demo deve usar insumos reais ou fixtures derivadas de conteúdos reais;
- o modo demo deve percorrer o fluxo real do sistema;
- o modo demo não deve apenas inserir um artigo final pronto sem passar pela esteira;
- o resultado do modo demo deve ser demonstrável ao vivo.

---

## Critério supremo de decisão

Em caso de dúvida entre duas abordagens técnicas, deve vencer a que melhor satisfaz simultaneamente estes quatro pontos:

1. maior chance de funcionar de verdade;
2. menor risco para a demo final;
3. menor complexidade acidental;
4. maior aderência aos requisitos obrigatórios do projeto.
