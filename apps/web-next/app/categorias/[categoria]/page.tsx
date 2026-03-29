import { notFound } from 'next/navigation';
import { getArticlesByCategory, type ArticleSummary } from '@/lib/api';
import { ArticleCard } from '@/components/ArticleCard';
import type { Metadata } from 'next';

interface Props {
  params: Promise<{ categoria: string }>;
}

const CATEGORIES: Record<string, { label: string; description: string }> = {
  oficial: {
    label: 'Notícias Oficiais',
    description:
      'Anúncios e atualizações publicados diretamente pelo WhatsApp — recursos confirmados e já disponíveis.',
  },
  beta: {
    label: 'Novidades Beta',
    description:
      'Funcionalidades em desenvolvimento identificadas pelo WABetaInfo. Ainda não lançadas oficialmente e sujeitas a mudanças.',
  },
};

export function generateStaticParams() {
  return Object.keys(CATEGORIES).map((categoria) => ({ categoria }));
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { categoria } = await params;
  const meta = CATEGORIES[categoria];
  if (!meta) return { title: 'Categoria não encontrada' };
  return {
    title: meta.label,
    description: meta.description,
  };
}

export default async function CategoryPage({ params }: Props) {
  const { categoria } = await params;

  if (!CATEGORIES[categoria]) notFound();

  const { label, description } = CATEGORIES[categoria];

  let articles: ArticleSummary[] = [];
  let error: string | null = null;

  try {
    articles = await getArticlesByCategory(categoria, 1, 20);
  } catch {
    error = 'Não foi possível carregar os artigos. Tente novamente em breve.';
  }

  return (
    <div>
      {/* Cabeçalho da categoria */}
      <header className="mb-8">
        <nav className="text-xs text-gray-400 mb-3">
          <a href="/" className="hover:text-gray-600 transition-colors">
            Início
          </a>
          <span className="mx-2">›</span>
          <span>{label}</span>
        </nav>

        <h1 className="text-3xl sm:text-4xl font-extrabold text-gray-900 leading-tight">
          {label}
        </h1>
        <p className="mt-3 text-gray-500 max-w-xl">{description}</p>

        {categoria === 'beta' && (
          <p className="mt-2 text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded px-3 py-1.5 inline-block">
            Conteúdo beta não representa anúncios oficiais do WhatsApp.
          </p>
        )}
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
          Nenhum artigo publicado nesta categoria ainda.
        </p>
      )}

      {/* Grid de artigos */}
      {articles.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-2">
          {articles.map((article) => (
            <ArticleCard key={article.id} article={article} />
          ))}
        </div>
      )}
    </div>
  );
}
