import type { ArticleSummary } from '@/lib/api';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { getDisplayTopics, topicToSlug } from '@/lib/topics';

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
}

interface Props {
  article: ArticleSummary;
  featured?: boolean;
}

export function ArticleCard({ article, featured = false }: Props) {
  const isBeta = article.articleType === 'BetaNews';
  const displayTopics = getDisplayTopics(article.topics ?? []);

  return (
    <a
      href={`/artigos/${article.slug}`}
      className={`group block ${featured ? 'md:col-span-2' : ''}`}
    >
      <Card className="h-full bg-white hover:shadow-lg hover:-translate-y-0.5 transition-all duration-200 border border-gray-100">
        <CardHeader className="pb-2">
          <div className="flex flex-wrap items-center gap-1.5 mb-2">
            {isBeta ? (
              <Badge className="bg-amber-100 text-amber-800 border-amber-200 hover:bg-amber-100">
                Beta
              </Badge>
            ) : (
              <Badge className="bg-green-100 text-green-800 border-green-200 hover:bg-green-100">
                Oficial
              </Badge>
            )}
            {displayTopics.map((topic) => (
              <span
                key={topic}
                className="inline-block px-2 py-0.5 rounded-full bg-blue-50 text-blue-700 text-[10px] font-medium border border-blue-100"
              >
                {topic}
              </span>
            ))}
          </div>
          <h2
            className={`font-bold leading-snug text-gray-900 group-hover:text-green-700 transition-colors ${
              featured ? 'text-2xl' : 'text-base'
            }`}
          >
            {article.title}
          </h2>
        </CardHeader>
        <CardContent>
          <p className="text-gray-600 text-sm leading-relaxed line-clamp-3 mb-3">
            {article.excerpt}
          </p>
          <time
            dateTime={article.publishedAt}
            className="text-xs text-gray-400"
          >
            {formatDate(article.publishedAt)}
          </time>
        </CardContent>
      </Card>
    </a>
  );
}
