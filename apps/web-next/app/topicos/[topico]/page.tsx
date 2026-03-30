import { notFound } from 'next/navigation';
import { getArticlesByTopic, type ArticleSummary } from '@/lib/api';
import { ArticleCard } from '@/components/ArticleCard';
import { TOPICS, slugToTopic } from '@/lib/topics';
import type { Metadata } from 'next';

interface Props {
  params: Promise<{ topico: string }>;
}

export function generateStaticParams() {
  return Object.keys(TOPICS).map((topico) => ({ topico }));
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { topico } = await params;
  const config = TOPICS[topico];
  if (!config) return { title: 'Tópico não encontrado' };
  return {
    title: `Artigos sobre ${config.label} — Portal WhatsApp News`,
    description: config.description,
    alternates: {
      canonical: `/topicos/${topico}`,
    },
    openGraph: {
      title: `Artigos sobre ${config.label}`,
      description: config.description,
    },
  };
}

export default async function TopicPage({ params }: Props) {
  const { topico } = await params;

  if (!TOPICS[topico]) notFound();

  const { label, description } = TOPICS[topico];
  const backendTopic = slugToTopic(topico);

  let articles: ArticleSummary[] = [];
  let error: string | null = null;

  try {
    if (backendTopic) {
      articles = await getArticlesByTopic(backendTopic, 1, 20);
    }
  } catch {
    error = 'Não foi possível carregar os artigos. Tente novamente em breve.';
  }

  return (
    <div>
      {/* Cabeçalho do tópico */}
      <header className="mb-8">
        <nav className="text-xs text-gray-400 mb-3">
          <a href="/" className="hover:text-gray-600 transition-colors">
            Início
          </a>
          <span className="mx-2">›</span>
          <span>Tópicos</span>
          <span className="mx-2">›</span>
          <span>{label}</span>
        </nav>

        <h1 className="text-3xl sm:text-4xl font-extrabold text-gray-900 leading-tight">
          {label}
        </h1>
        <p className="mt-3 text-gray-500 max-w-xl">{description}</p>
      </header>

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
          Nenhum artigo publicado neste tópico ainda.
        </p>
      )}

      {/* Grid de artigos */}
      {articles.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {articles.map((article) => (
            <ArticleCard key={article.id} article={article} />
          ))}
        </div>
      )}
    </div>
  );
}
