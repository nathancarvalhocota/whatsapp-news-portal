import type { ArticleSummary } from '@/lib/api';

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  });
}

interface Props {
  article: ArticleSummary;
  featured?: boolean;
}

export function ArticleCard({ article, featured = false }: Props) {
  const isBeta = article.articleType === 'BetaNews';

  return (
    <article
      className={`group bg-white border border-gray-200 rounded-xl overflow-hidden hover:shadow-md transition-shadow ${
        featured ? 'md:col-span-2' : ''
      }`}
    >
      <a href={`/artigos/${article.slug}`} className="block p-5 h-full">
        <div className="flex items-center gap-2 mb-3">
          {isBeta ? (
            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-amber-100 text-amber-800">
              Beta
            </span>
          ) : (
            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-green-100 text-green-800">
              Oficial
            </span>
          )}
          {article.category && (
            <span className="text-xs text-gray-400">{article.category}</span>
          )}
        </div>

        <h2
          className={`font-bold leading-snug group-hover:text-green-700 transition-colors ${
            featured ? 'text-xl' : 'text-base'
          }`}
        >
          {article.title}
        </h2>

        <p className="text-gray-600 text-sm mt-2 leading-relaxed line-clamp-3">
          {article.excerpt}
        </p>

        <time
          dateTime={article.publishedAt}
          className="block text-xs text-gray-400 mt-4"
        >
          {formatDate(article.publishedAt)}
        </time>
      </a>
    </article>
  );
}
