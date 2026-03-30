import '@testing-library/jest-dom';
import { render, screen } from '@testing-library/react';
import type { ArticleSummary } from '../lib/api';

// --- Mocks ---
jest.mock('../lib/api', () => ({
  getArticlesByCategory: jest.fn(),
}));

jest.mock('next/navigation', () => ({
  notFound: jest.fn(() => {
    throw new Error('NEXT_NOT_FOUND');
  }),
}));

jest.mock('next/headers', () => ({ headers: jest.fn(() => new Map()) }));

import * as api from '../lib/api';
import { notFound } from 'next/navigation';
import CategoryPage from '../app/categorias/[categoria]/page';

const mockArticle: ArticleSummary = {
  id: 'abc-1',
  slug: 'whatsapp-novidade-oficial',
  title: 'WhatsApp confirma novo recurso',
  excerpt: 'Resumo do novo recurso oficial.',
  metaDescription: 'Meta.',
  category: 'oficial',
  tags: ['whatsapp'],
  topics: ['Novos Recursos', 'Oficial'],
  articleType: 'OfficialNews',
  publishedAt: '2024-06-15T10:00:00Z',
};

const mockParams = (categoria: string) =>
  ({ params: Promise.resolve({ categoria }) }) as Parameters<typeof CategoryPage>[0];

describe('CategoryPage', () => {
  beforeEach(() => jest.clearAllMocks());

  it('renderiza o título da categoria "oficial"', async () => {
    (api.getArticlesByCategory as jest.Mock).mockResolvedValue([mockArticle]);
    const jsx = await CategoryPage(mockParams('oficial'));
    render(jsx);
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
      'Notícias Oficiais',
    );
  });

  it('renderiza o título da categoria "beta"', async () => {
    (api.getArticlesByCategory as jest.Mock).mockResolvedValue([]);
    const jsx = await CategoryPage(mockParams('beta'));
    render(jsx);
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
      'Novidades Beta',
    );
  });

  it('exibe os artigos quando retornados pela API', async () => {
    (api.getArticlesByCategory as jest.Mock).mockResolvedValue([mockArticle]);
    const jsx = await CategoryPage(mockParams('oficial'));
    render(jsx);
    expect(
      screen.getByRole('heading', { name: mockArticle.title }),
    ).toBeInTheDocument();
  });

  it('exibe mensagem de vazio quando não há artigos', async () => {
    (api.getArticlesByCategory as jest.Mock).mockResolvedValue([]);
    const jsx = await CategoryPage(mockParams('oficial'));
    render(jsx);
    expect(
      screen.getByText(/nenhum artigo publicado nesta categoria/i),
    ).toBeInTheDocument();
  });

  it('exibe erro quando a API falha', async () => {
    (api.getArticlesByCategory as jest.Mock).mockRejectedValue(
      new Error('timeout'),
    );
    const jsx = await CategoryPage(mockParams('oficial'));
    render(jsx);
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });

  it('chama notFound() para categoria desconhecida', async () => {
    await expect(
      CategoryPage(mockParams('inexistente')),
    ).rejects.toThrow('NEXT_NOT_FOUND');
    expect(notFound).toHaveBeenCalled();
  });

  it('exibe aviso beta na categoria beta', async () => {
    (api.getArticlesByCategory as jest.Mock).mockResolvedValue([]);
    const jsx = await CategoryPage(mockParams('beta'));
    render(jsx);
    expect(
      screen.getByText(/conteúdo beta não representa anúncios oficiais/i),
    ).toBeInTheDocument();
  });
});
