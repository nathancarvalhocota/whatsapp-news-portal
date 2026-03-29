import { getPublishedArticles, type ArticleSummary } from '@/lib/api';
import { ArticleCard } from '@/components/ArticleCard';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: { absolute: 'WhatsApp News — Portal de Notícias' },
  description:
    'Últimas notícias sobre o ecossistema WhatsApp em português: atualizações oficiais, novidades beta e muito mais.',
  alternates: {
    canonical: '/',
  },
};

export default async function HomePage() {
  let articles: ArticleSummary[] = [];
  let error: string | null = null;

  try {
    articles = await getPublishedArticles(1, 20);
  } catch {
    error = 'Não foi possível carregar os artigos. Tente novamente em breve.';
  }

  return (
    <div>
      {/* Hero */}
      <section className="mb-10">
        <h1 className="text-3xl sm:text-4xl font-extrabold text-gray-900 leading-tight">
          Últimas notícias sobre WhatsApp
        </h1>
        <p className="mt-3 text-gray-500 text-base sm:text-lg max-w-xl">
          Atualizações oficiais, recursos em beta e novidades do ecossistema WhatsApp — em português.
        </p>
      </section>

      {/* Error state */}
      {error && (
        <div
          role="alert"
          className="text-red-700 bg-red-50 border border-red-200 rounded-lg p-4 mb-8"
        >
          {error}
        </div>
      )}

      {/* Empty state */}
      {!error && articles.length === 0 && (
        <p className="text-gray-500 py-16 text-center">
          Nenhum artigo publicado ainda.
        </p>
      )}

      {/* Article grid */}
      {articles.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-2">
          {articles.map((article, index) => (
            <ArticleCard
              key={article.id}
              article={article}
              featured={index === 0}
            />
          ))}
        </div>
      )}
    </div>
  );
}
