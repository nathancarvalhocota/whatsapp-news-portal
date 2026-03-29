import type { MetadataRoute } from 'next';
import { getPublishedArticles } from '@/lib/api';

const siteUrl = process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000';

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const staticRoutes: MetadataRoute.Sitemap = [
    {
      url: siteUrl,
      changeFrequency: 'daily',
      priority: 1,
    },
    {
      url: `${siteUrl}/categorias/oficial`,
      changeFrequency: 'daily',
      priority: 0.8,
    },
    {
      url: `${siteUrl}/categorias/beta`,
      changeFrequency: 'daily',
      priority: 0.8,
    },
  ];

  let articleRoutes: MetadataRoute.Sitemap = [];
  try {
    const articles = await getPublishedArticles(1, 1000);
    articleRoutes = articles.map((article) => ({
      url: `${siteUrl}/artigos/${article.slug}`,
      lastModified: article.publishedAt,
      changeFrequency: 'weekly' as const,
      priority: 0.7,
    }));
  } catch {
    // API indisponível — retorna apenas rotas estáticas
  }

  return [...staticRoutes, ...articleRoutes];
}
