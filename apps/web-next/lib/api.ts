const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';

export interface ArticleSummary {
  id: string;
  slug: string;
  title: string;
  excerpt: string;
  metaDescription: string;
  category: string | null;
  tags: string[];
  topics: string[];
  articleType: string;
  publishedAt: string;
}

export interface SourceReference {
  sourceName: string;
  sourceUrl: string;
}

export interface ArticleDetail {
  id: string;
  slug: string;
  title: string;
  excerpt: string;
  contentHtml: string;
  metaTitle: string;
  metaDescription: string;
  schemaJsonLd: string | null;
  category: string | null;
  tags: string[];
  topics: string[];
  articleType: string;
  publishedAt: string;
  sourceReferences: SourceReference[];
}

async function apiFetch<T>(path: string, options?: RequestInit): Promise<T> {
  // Durante o build estático da Vercel usa timeout curto para não travar.
  // Em runtime (ISR / request) não limita — tolera o cold start do Render (~30s).
  const isBuildTime = process.env.NEXT_PHASE === 'phase-production-build';
  const signal = isBuildTime ? AbortSignal.timeout(8000) : undefined;

  const res = await fetch(`${API_URL}${path}`, {
    ...options,
    ...(signal ? { signal } : {}),
    headers: { 'Content-Type': 'application/json', ...options?.headers },
  });
  if (!res.ok) {
    throw new Error(`API error ${res.status}: ${path}`);
  }
  return res.json() as Promise<T>;
}

export async function getPublishedArticles(
  page = 1,
  pageSize = 20,
): Promise<ArticleSummary[]> {
  return apiFetch<ArticleSummary[]>(
    `/api/articles/published?page=${page}&pageSize=${pageSize}`,
    { next: { revalidate: 60 } },
  );
}

export async function getArticleBySlug(slug: string): Promise<ArticleDetail> {
  return apiFetch<ArticleDetail>(`/api/articles/${slug}`, {
    next: { revalidate: 60 },
  });
}

export async function getArticlesByCategory(
  category: string,
  page = 1,
  pageSize = 20,
): Promise<ArticleSummary[]> {
  return apiFetch<ArticleSummary[]>(
    `/api/categories/${category}?page=${page}&pageSize=${pageSize}`,
    { next: { revalidate: 60 } },
  );
}

export async function getArticlesByTopic(
  topic: string,
  page = 1,
  pageSize = 20,
): Promise<ArticleSummary[]> {
  return apiFetch<ArticleSummary[]>(
    `/api/articles/by-topic/${encodeURIComponent(topic)}?page=${page}&pageSize=${pageSize}`,
    { next: { revalidate: 60 } },
  );
}
