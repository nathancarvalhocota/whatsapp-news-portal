import '@testing-library/jest-dom';
import { render, screen } from '@testing-library/react';
import type { ArticleDetail } from '../lib/api';

// --- Mocks ---
jest.mock('../lib/api', () => ({
  getArticleBySlug: jest.fn(),
}));

jest.mock('next/navigation', () => ({
  notFound: jest.fn(() => {
    throw new Error('NEXT_NOT_FOUND');
  }),
}));

// next/headers e outras internals do Next que o RSC pode exigir
jest.mock('next/headers', () => ({ headers: jest.fn(() => new Map()) }));

import * as api from '../lib/api';
import { notFound } from 'next/navigation';
import ArticlePage from '../app/artigos/[slug]/page';

const mockArticle: ArticleDetail = {
  id: 'abc-123',
  slug: 'whatsapp-nova-funcionalidade',
  title: 'WhatsApp lança nova funcionalidade importante',
  excerpt: 'Resumo da nova funcionalidade para o público brasileiro.',
  contentHtml: '<h2>O que mudou</h2><p>O WhatsApp atualizou sua plataforma.</p>',
  metaTitle: 'WhatsApp lança nova funcionalidade | WhatsApp News',
  metaDescription: 'Meta description do artigo.',
  schemaJsonLd: null,
  category: 'oficial',
  tags: ['whatsapp', 'novidade'],
  topics: ['Novos Recursos', 'Oficial'],
  articleType: 'OfficialNews',
  publishedAt: '2024-06-15T10:00:00Z',
  sourceReferences: [
    { sourceName: 'WhatsApp Blog', sourceUrl: 'https://blog.whatsapp.com/post' },
  ],
};

const mockParams = (slug: string) =>
  ({ params: Promise.resolve({ slug }) }) as Parameters<typeof ArticlePage>[0];

describe('ArticlePage', () => {
  beforeEach(() => jest.clearAllMocks());

  it('renderiza o título do artigo', async () => {
    (api.getArticleBySlug as jest.Mock).mockResolvedValue(mockArticle);
    const jsx = await ArticlePage(mockParams('whatsapp-nova-funcionalidade'));
    render(jsx);
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
      mockArticle.title,
    );
  });

  it('renderiza o excerpt', async () => {
    (api.getArticleBySlug as jest.Mock).mockResolvedValue(mockArticle);
    const jsx = await ArticlePage(mockParams('whatsapp-nova-funcionalidade'));
    render(jsx);
    expect(screen.getByText(mockArticle.excerpt)).toBeInTheDocument();
  });

  it('exibe badge "Anúncio oficial" para artigo oficial', async () => {
    (api.getArticleBySlug as jest.Mock).mockResolvedValue(mockArticle);
    const jsx = await ArticlePage(mockParams('whatsapp-nova-funcionalidade'));
    render(jsx);
    expect(screen.getByText(/Anúncio oficial/i)).toBeInTheDocument();
  });

  it('exibe aviso beta para artigo BetaNews', async () => {
    (api.getArticleBySlug as jest.Mock).mockResolvedValue({
      ...mockArticle,
      articleType: 'BetaNews',
    });
    const jsx = await ArticlePage(mockParams('whatsapp-nova-funcionalidade'));
    render(jsx);
    expect(screen.getByText(/Beta \/ Em testes/i)).toBeInTheDocument();
    expect(screen.getByRole('note')).toBeInTheDocument();
  });

  it('exibe as fontes com link', async () => {
    (api.getArticleBySlug as jest.Mock).mockResolvedValue(mockArticle);
    const jsx = await ArticlePage(mockParams('whatsapp-nova-funcionalidade'));
    render(jsx);
    const link = screen.getByRole('link', { name: 'WhatsApp Blog' });
    expect(link).toHaveAttribute(
      'href',
      'https://blog.whatsapp.com/post',
    );
  });

  it('chama notFound() para artigo inexistente', async () => {
    (api.getArticleBySlug as jest.Mock).mockRejectedValue(
      new Error('API error 404'),
    );
    await expect(
      ArticlePage(mockParams('slug-inexistente')),
    ).rejects.toThrow('NEXT_NOT_FOUND');
    expect(notFound).toHaveBeenCalled();
  });
});
