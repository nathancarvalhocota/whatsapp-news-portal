export interface TopicConfig {
  label: string;
  slug: string;
  description: string;
}

/** All known topics — the slug is used in URLs, the label matches the back-end value exactly. */
export const TOPICS: Record<string, TopicConfig> = {
  'whatsapp-business': {
    label: 'WhatsApp Business',
    slug: 'whatsapp-business',
    description: 'Artigos sobre a plataforma WhatsApp Business e suas funcionalidades.',
  },
  'api-oficial': {
    label: 'API Oficial',
    slug: 'api-oficial',
    description: 'Artigos sobre a API oficial do WhatsApp Business.',
  },
  privacidade: {
    label: 'Privacidade',
    slug: 'privacidade',
    description: 'Artigos sobre privacidade e proteção de dados no WhatsApp.',
  },
  seguranca: {
    label: 'Segurança',
    slug: 'seguranca',
    description: 'Artigos sobre segurança e criptografia no WhatsApp.',
  },
  'novos-recursos': {
    label: 'Novos Recursos',
    slug: 'novos-recursos',
    description: 'Artigos sobre novos recursos e funcionalidades do WhatsApp.',
  },
  'dicas-de-uso': {
    label: 'Dicas de Uso',
    slug: 'dicas-de-uso',
    description: 'Dicas e tutoriais para aproveitar melhor o WhatsApp.',
  },
  atualizacoes: {
    label: 'Atualizações',
    slug: 'atualizacoes',
    description: 'Artigos sobre atualizações e versões do WhatsApp.',
  },
  oficial: {
    label: 'Oficial',
    slug: 'oficial',
    description: 'Artigos baseados em fontes oficiais do WhatsApp.',
  },
  betaspecialized: {
    label: 'BetaSpecialized',
    slug: 'betaspecialized',
    description: 'Artigos baseados em fontes beta especializadas como WABetaInfo.',
  },
};

/** Map from back-end topic label to URL slug */
export function topicToSlug(topic: string): string {
  const entry = Object.values(TOPICS).find((t) => t.label === topic);
  return entry?.slug ?? topic.toLowerCase().replace(/\s+/g, '-');
}

/** Map from URL slug to back-end topic label */
export function slugToTopic(slug: string): string | undefined {
  return TOPICS[slug]?.label;
}

/** Topics to display as badges (exclude the source-based Oficial/BetaSpecialized) */
export function getDisplayTopics(topics: string[]): string[] {
  return topics.filter((t) => t !== 'Oficial' && t !== 'BetaSpecialized');
}
