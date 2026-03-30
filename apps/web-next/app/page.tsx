import { getPublishedArticles, type ArticleSummary } from '@/lib/api';
import { ArticleCard } from '@/components/ArticleCard';
import { Separator } from '@/components/ui/separator';
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

  const oficial = articles.filter((a) => a.articleType !== 'BetaNews');
  const beta = articles.filter((a) => a.articleType === 'BetaNews');

  return (
    <div className="space-y-12">
      {/* Hero */}
      <section className="pt-4 pb-2">
        <p className="text-xs font-semibold tracking-widest text-green-600 uppercase mb-3">
          Portal de Notícias
        </p>
        <h1 className="text-4xl sm:text-5xl font-extrabold text-gray-900 leading-tight tracking-tight max-w-2xl">
          Tudo sobre o ecossistema WhatsApp
        </h1>
        <p className="mt-4 text-gray-500 text-lg max-w-xl leading-relaxed">
          Atualizações oficiais, recursos em beta e novidades. 
        </p>

        {articles.length > 0 && (
          <div className="flex flex-wrap gap-2.5 mt-6 text-sm">
            <a
              href="/categorias/oficial"
              className="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-green-50 text-green-800 font-medium hover:bg-green-100 transition-colors border border-green-200"
            >
              <span className="w-2 h-2 rounded-full bg-green-500 inline-block" />
              Notícias Oficiais
            </a>
            <a
              href="/categorias/beta"
              className="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-amber-50 text-amber-800 font-medium hover:bg-amber-100 transition-colors border border-amber-200"
            >
              <span className="w-2 h-2 rounded-full bg-amber-400 inline-block" />
              Recursos Beta
            </a>
            <a href="/topicos/novos-recursos" className="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-blue-50 text-blue-700 font-medium hover:bg-blue-100 transition-colors border border-blue-200">Novos Recursos</a>
            <a href="/topicos/whatsapp-business" className="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-blue-50 text-blue-700 font-medium hover:bg-blue-100 transition-colors border border-blue-200">WhatsApp Business</a>
            <a href="/topicos/seguranca" className="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-blue-50 text-blue-700 font-medium hover:bg-blue-100 transition-colors border border-blue-200">Seguranca</a>
            <a href="/topicos/privacidade" className="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-blue-50 text-blue-700 font-medium hover:bg-blue-100 transition-colors border border-blue-200">Privacidade</a>
            <a href="/topicos/atualizacoes" className="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-blue-50 text-blue-700 font-medium hover:bg-blue-100 transition-colors border border-blue-200">Atualizacoes</a>
            <a href="/topicos/api-oficial" className="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-blue-50 text-blue-700 font-medium hover:bg-blue-100 transition-colors border border-blue-200">API Oficial</a>
            <a href="/topicos/dicas-de-uso" className="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-blue-50 text-blue-700 font-medium hover:bg-blue-100 transition-colors border border-blue-200">Dicas de Uso</a>
          </div>
        )}
      </section>

      <Separator />

      {/* Error state */}
      {error && (
        <div
          role="alert"
          className="text-red-700 bg-red-50 border border-red-200 rounded-lg p-4"
        >
          {error}
        </div>
      )}

      {/* Empty state */}
      {!error && articles.length === 0 && (
        <div className="text-center py-20">
          <p className="text-gray-400 text-lg">Nenhum artigo publicado ainda.</p>
          <p className="text-gray-400 text-sm mt-2">
            Os artigos aparecerão aqui assim que forem publicados.
          </p>
        </div>
      )}

      {/* Todos os artigos — grid principal */}
      {articles.length > 0 && (
        <section>
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {articles.map((article, index) => (
              <ArticleCard
                key={article.id}
                article={article}
                featured={index === 0}
              />
            ))}
          </div>
        </section>
      )}

      {/* Seção Oficial */}
      {oficial.length > 0 && (
        <section>
          <div className="flex items-center gap-3 mb-6">
            <span className="w-3 h-3 rounded-full bg-green-500 flex-shrink-0" />
            <h2 className="text-2xl font-bold text-gray-900">
              Anúncios Oficiais
            </h2>
            <span className="ml-auto text-sm text-gray-400">
              {oficial.length} artigo{oficial.length !== 1 ? 's' : ''}
            </span>
          </div>
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {oficial.slice(0, 6).map((article) => (
              <ArticleCard key={article.id} article={article} />
            ))}
          </div>
          {oficial.length > 6 && (
            <div className="mt-6 text-center">
              <a
                href="/categorias/oficial"
                className="text-sm text-green-700 hover:text-green-900 font-medium hover:underline"
              >
                Ver todos os {oficial.length} artigos oficiais →
              </a>
            </div>
          )}
        </section>
      )}

      {/* Seção Beta */}
      {beta.length > 0 && (
        <section>
          <div className="flex items-center gap-3 mb-6">
            <span className="w-3 h-3 rounded-full bg-amber-400 flex-shrink-0" />
            <h2 className="text-2xl font-bold text-gray-900">
              Recursos em Beta
            </h2>
            <span className="ml-auto text-sm text-gray-400">
              {beta.length} artigo{beta.length !== 1 ? 's' : ''}
            </span>
          </div>
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {beta.slice(0, 6).map((article) => (
              <ArticleCard key={article.id} article={article} />
            ))}
          </div>
          {beta.length > 6 && (
            <div className="mt-6 text-center">
              <a
                href="/categorias/beta"
                className="text-sm text-amber-700 hover:text-amber-900 font-medium hover:underline"
              >
                Ver todos os {beta.length} artigos beta →
              </a>
            </div>
          )}
        </section>
      )}
    </div>
  );
}
