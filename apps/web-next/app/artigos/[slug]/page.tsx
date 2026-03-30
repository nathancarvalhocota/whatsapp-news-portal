import { notFound } from 'next/navigation';
import { getArticleBySlug } from '@/lib/api';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { getDisplayTopics, topicToSlug } from '@/lib/topics';
import type { Metadata } from 'next';

const siteUrl = process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000';

interface Props {
  params: Promise<{ slug: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  try {
    const { slug } = await params;
    const article = await getArticleBySlug(slug);
    const title = article.metaTitle || article.title;
    return {
      title,
      description: article.metaDescription,
      alternates: {
        canonical: `/artigos/${slug}`,
      },
      openGraph: {
        type: 'article',
        title,
        description: article.metaDescription,
        publishedTime: article.publishedAt,
      },
    };
  } catch {
    return { title: 'Artigo não encontrado' };
  }
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  });
}

export default async function ArticlePage({ params }: Props) {
  const { slug } = await params;

  let article;
  try {
    article = await getArticleBySlug(slug);
  } catch {
    notFound();
  }

  const isBeta = article.articleType === 'BetaNews';

  return (
    <>
      {/* JSON-LD */}
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html:
            article.schemaJsonLd ??
            JSON.stringify({
              '@context': 'https://schema.org',
              '@type': 'NewsArticle',
              headline: article.title,
              description: article.metaDescription,
              datePublished: article.publishedAt,
              author: { '@type': 'Organization', name: 'WhatsApp News' },
              publisher: { '@type': 'Organization', name: 'WhatsApp News' },
              mainEntityOfPage: {
                '@type': 'WebPage',
                '@id': `${siteUrl}/artigos/${slug}`,
              },
            }),
        }}
      />

      <article className="max-w-3xl mx-auto">
        {/* Cabeçalho */}
        <header className="mb-8">
          {/* Breadcrumb */}
          <nav className="text-sm text-gray-400 mb-6">
            <a href="/" className="hover:text-gray-600 transition-colors">
              Início
            </a>
            {article.category && (
              <>
                <span className="mx-2">›</span>
                <a
                  href={`/categorias/${article.category}`}
                  className="hover:text-gray-600 transition-colors capitalize"
                >
                  {article.category}
                </a>
              </>
            )}
            <span className="mx-2">›</span>
            <span className="text-gray-500 line-clamp-1">{article.title}</span>
          </nav>

          {/* Título principal */}
          <h1 className="text-3xl sm:text-4xl font-bold text-gray-900 leading-tight mb-4">
            {article.title}
          </h1>

          {/* Badge e metadados */}
          <div className="flex flex-wrap items-center gap-2 mb-4">
            {isBeta ? (
              <Badge className="bg-amber-100 text-amber-800 border-amber-200 hover:bg-amber-100">
                Beta / Em testes
              </Badge>
            ) : (
              <Badge className="bg-green-100 text-green-800 border-green-200 hover:bg-green-100">
                Anúncio Oficial
              </Badge>
            )}
            {getDisplayTopics(article.topics ?? []).map((topic) => (
              <a
                key={topic}
                href={`/topicos/${topicToSlug(topic)}`}
                className="inline-block px-2.5 py-0.5 rounded-full bg-blue-50 text-blue-700 text-xs font-medium border border-blue-100 hover:bg-blue-100 transition-colors"
              >
                {topic}
              </a>
            ))}
            <time
              dateTime={article.publishedAt}
              className="text-sm text-gray-400 ml-auto"
            >
              {formatDate(article.publishedAt)}
            </time>
          </div>

          {/* Resumo */}
          <p className="text-lg text-gray-600 leading-relaxed border-l-4 border-green-200 pl-4">
            {article.excerpt}
          </p>
        </header>

        {/* Aviso beta */}
        {isBeta && (
          <aside
            role="note"
            className="mb-8 p-4 rounded-lg border border-amber-200 bg-amber-50 text-amber-900 text-sm"
          >
            <strong>Atenção:</strong> Este conteúdo é baseado em informações do{' '}
            <em>estágio beta</em> do WhatsApp. O recurso ainda não foi lançado
            oficialmente e pode sofrer alterações ou não chegar a todos os
            usuários.
          </aside>
        )}

        <Separator className="mb-8" />

        {/* Corpo do artigo */}
        <div
          className="prose prose-lg prose-gray max-w-none prose-headings:font-bold prose-a:text-green-700 prose-a:no-underline hover:prose-a:underline"
          dangerouslySetInnerHTML={{ __html: article.contentHtml }}
        />

        {/* Rodapé do artigo */}
        <footer className="mt-12 pt-8 border-t border-gray-200 space-y-6">
          {/* Tags */}
          {article.tags.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {article.tags.map((tag) => (
                <span
                  key={tag}
                  className="px-2.5 py-1 rounded-full bg-gray-100 text-gray-600 text-xs font-medium"
                >
                  #{tag}
                </span>
              ))}
            </div>
          )}

          {/* Fontes */}
          {article.sourceReferences.length > 0 && (
            <div>
              <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-widest mb-3">
                Fonte{article.sourceReferences.length > 1 ? 's' : ''} original
                {article.sourceReferences.length > 1 ? 'is' : ''}
              </h2>
              <ul className="space-y-2">
                {article.sourceReferences.map((ref) => (
                  <li key={ref.sourceUrl}>
                    <a
                      href={ref.sourceUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-1.5 text-sm text-green-700 hover:text-green-900 hover:underline font-medium"
                    >
                      {ref.sourceName}
                      <svg
                        xmlns="http://www.w3.org/2000/svg"
                        width="12"
                        height="12"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        aria-hidden="true"
                      >
                        <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6" />
                        <polyline points="15 3 21 3 21 9" />
                        <line x1="10" y1="14" x2="21" y2="3" />
                      </svg>
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {/* Voltar */}
          <div>
            <a
              href="/"
              className="inline-flex items-center gap-1.5 text-sm text-gray-500 hover:text-gray-800 transition-colors font-medium"
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                width="14"
                height="14"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                aria-hidden="true"
              >
                <line x1="19" y1="12" x2="5" y2="12" />
                <polyline points="12 19 5 12 12 5" />
              </svg>
              Voltar para a página inicial
            </a>
          </div>
        </footer>
      </article>
    </>
  );
}
