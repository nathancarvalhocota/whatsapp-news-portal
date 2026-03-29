/** @jest-environment node */
// Smoke test: verifica que o módulo da API exporta as funções esperadas.
import {
  getPublishedArticles,
  getArticleBySlug,
  getArticlesByCategory,
} from '../lib/api';

describe('lib/api smoke test', () => {
  it('exports getPublishedArticles as a function', () => {
    expect(typeof getPublishedArticles).toBe('function');
  });

  it('exports getArticleBySlug as a function', () => {
    expect(typeof getArticleBySlug).toBe('function');
  });

  it('exports getArticlesByCategory as a function', () => {
    expect(typeof getArticlesByCategory).toBe('function');
  });
});
