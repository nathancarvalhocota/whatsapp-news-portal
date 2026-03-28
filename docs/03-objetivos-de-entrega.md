# Objetivos de Entrega

## 1. Objetivo deste documento
Este documento define o que a entrega final precisa demonstrar, quais resultados mínimos devem existir ao fim do hackathon e quais critérios determinam se a solução está pronta para ser apresentada aos jurados.

Este documento não descreve tarefas de implementação detalhadas. Ele define resultados esperados, limites de escopo e critérios de sucesso.

---

## 2. Objetivo principal da entrega
Ao final do hackathon, deve existir um portal público, acessível pela internet, que demonstre com clareza que o projeto funciona de ponta a ponta.

A entrega precisa provar que o sistema é capaz de:
1. monitorar fontes confiáveis do ecossistema WhatsApp;
2. descobrir conteúdo novo ou simulado de forma controlada;
3. tratar esse conteúdo editorialmente;
4. gerar artigo original em PT-BR com apoio de IA;
5. publicar o artigo no portal;
6. disponibilizar esse conteúdo com SEO técnico mínimo adequado.

---

## 3. Resultado final obrigatório
A entrega final deve conter, obrigatoriamente:
- portal publicado e acessível publicamente;
- artigos reais já publicados no portal;
- artigos obtidos diretamente das fontes definidas no escopo;
- distinção clara entre conteúdo oficial e conteúdo beta;
- funcionamento demonstrável do pipeline ponta a ponta;
- modo demo funcional para reproduzir o fluxo mesmo sem novidade real nas fontes.

---

## 4. Objetivos de produto

### 4.1 Objetivo editorial
Demonstrar que o sistema gera conteúdo útil e original, e não apenas tradução ou cópia dos posts originais.

### 4.2 Objetivo técnico
Demonstrar que existe um pipeline funcional, com persistência real, estados coerentes e publicação efetiva.

### 4.3 Objetivo de negócio
Demonstrar a viabilidade de um canal de aquisição orgânica baseado em conteúdo especializado sobre WhatsApp.

### 4.4 Objetivo de demonstração
Garantir que o jurado consiga acessar o produto e observar resultados reais, sem depender de explicações abstratas ou partes “ainda não integradas”.

---

## 5. Objetivos mínimos por área

## 5.1 Front-end
A entrega do front-end deve demonstrar, no mínimo:
- home pública funcionando;
- páginas de artigo publicadas;
- navegação básica coerente;
- experiência visual limpa e responsiva;
- metadados e elementos de SEO técnico essenciais.

### 5.2 Back-end
A entrega do back-end deve demonstrar, no mínimo:
- descoberta de conteúdo;
- processamento do item;
- geração de draft;
- persistência em banco;
- publicação;
- tolerância a falhas básicas;
- execução manual do modo demo.

### 5.3 Banco de dados
A entrega deve demonstrar persistência real de dados relevantes, ao menos para:
- fontes;
- itens descobertos;
- estado do pipeline;
- artigos;
- dados mínimos de publicação.

### 5.4 IA
A entrega deve demonstrar uso real da IA para:
- tratamento/classificação inicial;
- geração de metadados editoriais;
- geração do artigo final em PT-BR.

### 5.5 Deploy
A entrega deve demonstrar:
- front publicado na Vercel;
- backend publicado no Render;
- banco em Render Postgres;
- portal acessível ao jurado no momento da avaliação.

---

## 6. Critérios de sucesso do hackathon

O projeto será considerado bem-sucedido se, ao final, for possível demonstrar com clareza:

### 6.1 Sucesso funcional
- descoberta de um item de fonte;
- entrada do item na esteira;
- passagem por estados coerentes;
- geração de draft;
- geração final do artigo;
- publicação no site.

### 6.2 Sucesso editorial
- artigo em PT-BR;
- artigo original;
- contexto útil para público brasileiro;
- distinção correta entre oficial e beta;
- ausência de aparência de “post gerado sem curadoria mínima”.

### 6.3 Sucesso técnico
- sistema funcionando com dados reais;
- banco persistindo dados reais;
- site acessível externamente;
- pipeline demonstrável;
- logs ou sinais mínimos para explicar o funcionamento.

### 6.4 Sucesso de apresentação
- demo previsível;
- narrativa simples;
- fluxo entendível;
- prova concreta do valor do projeto.

---

## 7. Entrega mínima aceitável

A entrega mínima aceitável deve conter:
- pelo menos um conjunto de artigos publicados oriundos das fontes definidas;
- pelo menos um fluxo ponta a ponta demonstrável via modo normal ou modo demo;
- páginas públicas funcionais;
- metadados essenciais de SEO;
- backend e banco em funcionamento;
- regras editoriais corretas para WABetaInfo.

Se esses elementos existirem e forem demonstráveis, a entrega pode ser considerada válida mesmo sem recursos adicionais.

---

## 8. O que é mais importante do que sofisticação

Para esta entrega, têm prioridade superior:
- funcionamento real;
- clareza da demo;
- robustez mínima;
- publicação pública;
- conteúdo real no portal.

Não têm prioridade superior:
- sofisticação arquitetural excessiva;
- automação além do necessário;
- features paralelas à proposta central;
- complexidade não demonstrável.

---

## 9. Objetivos de demo

A demonstração final deve ser capaz de mostrar:

### 9.1 Cenário 1 — navegação pública
O jurado acessa o portal e consegue ver:
- home;
- artigos publicados;
- organização editorial mínima;
- distinção entre oficial e beta;
- páginas responsivas e renderizadas adequadamente.

### 9.2 Cenário 2 — prova do pipeline
Deve ser possível demonstrar que um item:
- foi encontrado;
- foi tratado;
- gerou draft;
- gerou artigo;
- foi publicado.

### 9.3 Cenário 3 — contingência
Caso não exista novidade real recente na janela do evento, deve ser possível executar `RunDemoPipeline`, ou mecanismo equivalente, usando insumos reais controlados, para demonstrar o mesmo fluxo sem depender do acaso.

---

## 10. Objetivos de qualidade mínima

A entrega deve buscar garantir, no mínimo:
- consistência visual;
- consistência editorial;
- ausência de erros graves de navegação;
- ausência de falhas óbvias de estado do pipeline;
- ausência de mistura indevida entre beta e oficial;
- ausência de conteúdo claramente copiado da fonte original.

---

## 11. Objetivos de prontidão para avaliação

Antes da avaliação, o sistema deve estar:
- publicado;
- populado com artigos reais;
- configurado com URLs finais acessíveis;
- validado manualmente em navegação básica;
- validado no modo demo;
- preparado para cold start do backend, se necessário.

---

## 12. Objetivos de comunicação do valor

A entrega deve deixar evidente, mesmo para quem não conhece a implementação interna, que:
- há um problema real de mercado;
- as fontes escolhidas fazem sentido;
- o tratamento editorial gera valor adicional;
- o portal pode capturar tráfego orgânico relevante;
- a solução é viável como MVP real.

---

## 13. Não objetivos nesta etapa

Não são objetivos obrigatórios desta entrega:
- painel administrativo completo;
- moderação editorial complexa;
- múltiplos provedores de IA;
- alta escalabilidade de produção;
- automações empresariais avançadas;
- busca pública avançada;
- clusterização sofisticada multi-evento;
- infraestrutura enterprise.

Esses itens só devem existir se não comprometerem o mínimo funcional.

---

## 14. Checklist final de sucesso

A entrega estará pronta para avaliação quando for possível responder “sim” para todas as perguntas abaixo:

1. O portal está publicado e acessível publicamente?
2. Existem artigos reais já publicados?
3. Esses artigos vieram das fontes definidas?
4. O conteúdo está em PT-BR e não parece mera tradução?
5. WABetaInfo está tratado explicitamente como beta?
6. O pipeline tem estados coerentes?
7. Existe draft como etapa real do fluxo?
8. O modo demo funciona?
9. A home e as páginas de artigo funcionam?
10. O site possui SEO técnico mínimo implementado?
11. A demo consegue provar o valor do projeto sem depender de improviso?

Se todas as respostas forem positivas, a entrega está adequada ao objetivo do hackathon.
