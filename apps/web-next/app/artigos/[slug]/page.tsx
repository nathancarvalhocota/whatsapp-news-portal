import { notFound } from 'next/navigation';
import { getArticleBySlug } from '@/lib/api';
import type { Metadata } from 'next';

interface Props {
  params: Promise<{ slug: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  try {
    const { slug } = await params;
    const article = await getArticleBySlug(slug);
    return {
      title: article.metaTitle || article.title,
      description: article.metaDescription,
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
      {/* JSON-LD injetado pelo back-end */}
      {article.schemaJsonLd && (
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: article.schemaJsonLd }}
        />
      )}

      <article>
        {/* Cabeçalho */}
        <header className="mb-8">
          {/* Badges */}
          <div className="flex flex-wrap items-center gap-2 mb-4">
            {isBeta ? (
              <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-100 text-amber-800 border border-amber-200">
                ⚠ Beta / Em testes
              </span>
            ) : (
              <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold bg-green-100 text-green-800 border border-green-200">
                ✓ Anúncio oficial
              </span>
            )}
            {article.category && (
              <span className="text-xs text-gray-500 uppercase tracking-wide">
                {article.category}
              </span>
            )}
          </div>

          {/* Título principal */}
          <h1 className="text-3xl sm:text-4xl font-extrabold text-gray-900 leading-tight">
            {article.title}
          </h1>

          {/* Resumo */}
          <p className="mt-4 text-lg text-gray-600 leading-relaxed">
            {article.excerpt}
          </p>

          {/* Data */}
          <time
            dateTime={article.publishedAt}
            className="block mt-4 text-sm text-gray-400"
          >
            Publicado em {formatDate(article.publishedAt)}
          </time>
        </header>

        {/* Aviso beta destacado */}
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

        {/* Separador */}
        <hr className="border-gray-200 mb-8" />

        {/* Corpo do artigo */}
        <div
          className="prose"
          dangerouslySetInnerHTML={{ __html: article.contentHtml }}
        />

        {/* Rodapé do artigo */}
        <footer className="mt-12 pt-6 border-t border-gray-200 space-y-6">
          {/* Tags */}
          {article.tags.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {article.tags.map((tag) => (
                <span
                  key={tag}
                  className="px-2 py-0.5 rounded bg-gray-100 text-gray-600 text-xs"
                >
                  #{tag}
                </span>
              ))}
            </div>
          )}

          {/* Fontes */}
          {article.sourceReferences.length > 0 && (
            <div>
              <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wide mb-2">
                Fontes
              </h2>
              <ul className="space-y-1">
                {article.sourceReferences.map((ref) => (
                  <li key={ref.sourceUrl} className="text-sm">
                    <a
                      href={ref.sourceUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-green-700 hover:underline"
                    >
                      {ref.sourceName}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {/* Voltar */}
          <div>
            <a href="/" className="text-sm text-gray-500 hover:text-gray-800 transition-colors">
              ← Voltar para a página inicial
            </a>
          </div>
        </footer>
      </article>
    </>
  );
}
