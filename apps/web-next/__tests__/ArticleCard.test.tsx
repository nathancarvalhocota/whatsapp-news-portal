import '@testing-library/jest-dom';
import { render, screen } from '@testing-library/react';
import { ArticleCard } from '../components/ArticleCard';
import type { ArticleSummary } from '../lib/api';

const base: ArticleSummary = {
  id: 'abc-123',
  slug: 'whatsapp-novidade',
  title: 'WhatsApp lança nova funcionalidade',
  excerpt: 'Descrição da novidade para o público brasileiro.',
  metaDescription: 'Meta description',
  category: 'oficial',
  tags: ['whatsapp'],
  topics: ['Novos Recursos', 'Oficial'],
  articleType: 'OfficialNews',
  publishedAt: '2024-06-15T10:00:00Z',
};

describe('ArticleCard', () => {
  it('renderiza o título do artigo', () => {
    render(<ArticleCard article={base} />);
    expect(screen.getByRole('heading', { name: base.title })).toBeInTheDocument();
  });

  it('renderiza o excerpt', () => {
    render(<ArticleCard article={base} />);
    expect(screen.getByText(base.excerpt)).toBeInTheDocument();
  });

  it('exibe badge "Oficial" para artigo oficial', () => {
    render(<ArticleCard article={base} />);
    expect(screen.getByText('Oficial')).toBeInTheDocument();
  });

  it('exibe badge "Beta" para artigo beta', () => {
    render(<ArticleCard article={{ ...base, articleType: 'BetaNews' }} />);
    expect(screen.getByText('Beta')).toBeInTheDocument();
  });

  it('o link aponta para /artigos/[slug]', () => {
    render(<ArticleCard article={base} />);
    const link = screen.getByRole('link');
    expect(link).toHaveAttribute('href', `/artigos/${base.slug}`);
  });

  it('exibe badges de tópicos temáticos', () => {
    render(<ArticleCard article={base} />);
    expect(screen.getByText('Novos Recursos')).toBeInTheDocument();
  });

  it('não exibe tópicos de fonte como badge extra', () => {
    render(<ArticleCard article={base} />);
    const badges = screen.queryAllByText('Oficial');
    expect(badges).toHaveLength(1); // apenas a badge verde, não o tópico
  });
});
