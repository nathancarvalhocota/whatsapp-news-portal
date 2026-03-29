'use client';

import { useState, useCallback, useEffect } from 'react';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5074';
const ADMIN_SECRET = process.env.NEXT_PUBLIC_ADMIN_SECRET ?? 'hackathon2025';

interface Draft {
  id: string;
  slug: string;
  title: string;
  articleType: string;
  category: string | null;
  createdAt: string;
}

interface DemoResult {
  success: boolean;
  url: string;
  sourceName?: string;
  sourceItemId?: string;
  articleId?: string;
  slug?: string;
  status?: string;
  errorMessage?: string;
  steps: string[];
  executedAt: string;
}

interface PipelineSettings {
  intervalMinutes: number;
  minPublishedDate: string;
  autoPublishDrafts: boolean;
}

function StatusBadge({ status }: { status: string }) {
  const colors: Record<string, string> = {
    Draft: 'bg-yellow-100 text-yellow-800',
    Published: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800',
    Processing: 'bg-blue-100 text-blue-800',
  };
  return (
    <span className={`px-2 py-0.5 rounded text-xs font-medium ${colors[status] ?? 'bg-gray-100 text-gray-700'}`}>
      {status}
    </span>
  );
}

export default function AdminPage() {
  const [password, setPassword] = useState('');
  const [authenticated, setAuthenticated] = useState(false);
  const [authError, setAuthError] = useState('');

  // Settings
  const [settings, setSettings] = useState<PipelineSettings | null>(null);
  const [settingsForm, setSettingsForm] = useState({ intervalMinutes: '', minPublishedDate: '', autoPublishDrafts: true });
  const [settingsLoading, setSettingsLoading] = useState(false);
  const [settingsSaving, setSettingsSaving] = useState(false);
  const [settingsMsg, setSettingsMsg] = useState('');

  // Demo pipeline
  const [demoUrl, setDemoUrl] = useState('');
  const [demoLoading, setDemoLoading] = useState(false);
  const [demoResult, setDemoResult] = useState<DemoResult | null>(null);
  const [demoError, setDemoError] = useState('');

  // Drafts
  const [drafts, setDrafts] = useState<Draft[]>([]);
  const [draftsLoading, setDraftsLoading] = useState(false);
  const [draftsError, setDraftsError] = useState('');
  const [publishingId, setPublishingId] = useState<string | null>(null);
  const [publishResults, setPublishResults] = useState<Record<string, string>>({});

  function handleLogin() {
    if (password === ADMIN_SECRET) {
      setAuthenticated(true);
      setAuthError('');
    } else {
      setAuthError('Senha incorreta.');
    }
  }

  const loadSettings = useCallback(async () => {
    setSettingsLoading(true);
    try {
      const res = await fetch(`${API_URL}/api/settings/pipeline`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data: PipelineSettings = await res.json();
      setSettings(data);
      setSettingsForm({
        intervalMinutes: String(data.intervalMinutes),
        minPublishedDate: data.minPublishedDate,
        autoPublishDrafts: data.autoPublishDrafts,
      });
    } catch {
      setSettingsMsg('Erro ao carregar configurações.');
    } finally {
      setSettingsLoading(false);
    }
  }, []);

  const loadDrafts = useCallback(async () => {
    setDraftsLoading(true);
    setDraftsError('');
    try {
      const res = await fetch(`${API_URL}/api/articles/drafts`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      setDrafts(await res.json());
    } catch (e) {
      setDraftsError(`Erro ao carregar drafts: ${e instanceof Error ? e.message : e}`);
    } finally {
      setDraftsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (authenticated) {
      loadSettings();
      loadDrafts();
    }
  }, [authenticated, loadSettings, loadDrafts]);

  async function saveSettings() {
    setSettingsSaving(true);
    setSettingsMsg('');
    try {
      const res = await fetch(`${API_URL}/api/settings/pipeline`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          intervalMinutes: settingsForm.intervalMinutes ? Number(settingsForm.intervalMinutes) : undefined,
          minPublishedDate: settingsForm.minPublishedDate || undefined,
          autoPublishDrafts: settingsForm.autoPublishDrafts,
        }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error ?? `HTTP ${res.status}`);
      setSettings(data);
      setSettingsMsg('Salvo com sucesso.');
      setTimeout(() => setSettingsMsg(''), 3000);
    } catch (e) {
      setSettingsMsg(`Erro: ${e instanceof Error ? e.message : e}`);
    } finally {
      setSettingsSaving(false);
    }
  }

  async function runDemo() {
    if (!demoUrl.trim()) return;
    setDemoLoading(true);
    setDemoError('');
    setDemoResult(null);
    try {
      const res = await fetch(`${API_URL}/api/pipeline/run-demo`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url: demoUrl.trim(), reset: false }),
      });
      const data: DemoResult = await res.json();
      setDemoResult(data);
      if (data.success) loadDrafts();
    } catch (e) {
      setDemoError(`Erro: ${e instanceof Error ? e.message : e}`);
    } finally {
      setDemoLoading(false);
    }
  }

  async function publishArticle(id: string) {
    setPublishingId(id);
    try {
      const res = await fetch(`${API_URL}/api/articles/${id}/publish`, { method: 'POST' });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error ?? `HTTP ${res.status}`);
      setPublishResults((prev) => ({ ...prev, [id]: data.slug ?? 'publicado' }));
      loadDrafts();
      if (demoResult?.articleId === id) {
        setDemoResult((prev) => prev ? { ...prev, status: 'Published' } : prev);
      }
    } catch (e) {
      setPublishResults((prev) => ({
        ...prev,
        [id]: `Erro: ${e instanceof Error ? e.message : e}`,
      }));
    } finally {
      setPublishingId(null);
    }
  }

  if (!authenticated) {
    return (
      <div className="max-w-sm mx-auto mt-24 p-6 border border-gray-200 rounded-xl">
        <h1 className="text-xl font-bold text-gray-900 mb-6">Operador — Acesso Admin</h1>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleLogin()}
          placeholder="Senha"
          className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm mb-3 focus:outline-none focus:ring-2 focus:ring-green-500"
        />
        {authError && <p className="text-red-600 text-sm mb-3">{authError}</p>}
        <button
          onClick={handleLogin}
          className="w-full bg-green-600 hover:bg-green-700 text-white font-medium py-2 rounded-lg text-sm transition-colors"
        >
          Entrar
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-8 max-w-2xl">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Operador — Demo</h1>
        <button
          onClick={() => setAuthenticated(false)}
          className="text-xs text-gray-400 hover:text-gray-600"
        >
          Sair
        </button>
      </div>

      {/* Pipeline Settings */}
      <section className="border border-gray-200 rounded-xl p-5">
        <h2 className="font-semibold text-gray-800 mb-3">Configurações do Pipeline</h2>
        <p className="text-sm text-gray-500 mb-4">
          Valores atuais do job que roda periodicamente. Alterações aplicam no próximo ciclo.
        </p>
        {settingsLoading ? (
          <p className="text-sm text-gray-400">Carregando...</p>
        ) : (
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-500 mb-1">
                  Intervalo (minutos)
                </label>
                <input
                  type="number"
                  min="1"
                  value={settingsForm.intervalMinutes}
                  onChange={(e) => setSettingsForm((f) => ({ ...f, intervalMinutes: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-500 mb-1">
                  Data mínima (corte)
                </label>
                <input
                  type="date"
                  value={settingsForm.minPublishedDate}
                  onChange={(e) => setSettingsForm((f) => ({ ...f, minPublishedDate: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                />
              </div>
            </div>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={settingsForm.autoPublishDrafts}
                onChange={(e) => setSettingsForm((f) => ({ ...f, autoPublishDrafts: e.target.checked }))}
                className="rounded border-gray-300 text-green-600 focus:ring-green-500"
              />
              <span className="text-sm text-gray-700">Publicar automaticamente novos artigos</span>
            </label>
            <div className="flex items-center gap-3">
              <button
                onClick={saveSettings}
                disabled={settingsSaving}
                className="bg-gray-800 hover:bg-gray-900 disabled:opacity-50 text-white font-medium px-4 py-2 rounded-lg text-sm transition-colors"
              >
                {settingsSaving ? 'Salvando...' : 'Salvar'}
              </button>
              {settingsMsg && (
                <span className={`text-sm ${settingsMsg.startsWith('Erro') ? 'text-red-600' : 'text-green-600'}`}>
                  {settingsMsg}
                </span>
              )}
              {settings && (
                <span className="text-xs text-gray-400 ml-auto">
                  Atual: {settings.intervalMinutes}min / {settings.minPublishedDate} / {settings.autoPublishDrafts ? 'auto-pub' : 'manual'}
                </span>
              )}
            </div>
          </div>
        )}
      </section>

      {/* Demo Pipeline */}
      <section className="border border-gray-200 rounded-xl p-5">
        <h2 className="font-semibold text-gray-800 mb-3">Demo Pipeline</h2>
        <p className="text-sm text-gray-500 mb-4">
          Processa uma URL específica e gera artigo via Gemini.
        </p>
        <div className="flex gap-2">
          <input
            type="url"
            value={demoUrl}
            onChange={(e) => setDemoUrl(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && runDemo()}
            placeholder="https://..."
            className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
          />
          <button
            onClick={runDemo}
            disabled={demoLoading || !demoUrl.trim()}
            className="bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white font-medium px-4 py-2 rounded-lg text-sm transition-colors whitespace-nowrap"
          >
            {demoLoading ? 'Rodando...' : 'Rodar Demo'}
          </button>
        </div>
        {demoError && (
          <p className="mt-3 text-red-600 text-sm">{demoError}</p>
        )}
        {demoResult && (
          <div className={`mt-3 rounded-lg p-3 text-sm space-y-1 ${demoResult.success ? 'bg-green-50 text-green-900' : 'bg-red-50 text-red-900'}`}>
            <div className="flex items-center gap-2">
              <span className="font-medium">{demoResult.success ? 'Sucesso' : 'Falhou'}</span>
              {demoResult.status && <StatusBadge status={demoResult.status} />}
            </div>
            {demoResult.sourceName && <p>Fonte: {demoResult.sourceName}</p>}
            {demoResult.slug && <p>Slug: <code className="font-mono text-xs">{demoResult.slug}</code></p>}
            {demoResult.articleId && (
              <p>Article ID: <code className="font-mono text-xs break-all">{demoResult.articleId}</code></p>
            )}
            {demoResult.errorMessage && <p className="text-red-700">{demoResult.errorMessage}</p>}
            {demoResult.steps.length > 0 && (
              <ul className="list-disc list-inside space-y-0.5 mt-1">
                {demoResult.steps.map((s, i) => <li key={i}>{s}</li>)}
              </ul>
            )}
            {demoResult.articleId && demoResult.status === 'Draft' && (
              <button
                onClick={() => publishArticle(demoResult.articleId!)}
                disabled={publishingId === demoResult.articleId}
                className="mt-2 bg-green-700 hover:bg-green-800 disabled:opacity-50 text-white font-medium px-3 py-1.5 rounded-lg text-xs transition-colors"
              >
                {publishingId === demoResult.articleId ? 'Publicando...' : 'Publicar este artigo'}
              </button>
            )}
            {demoResult.articleId && publishResults[demoResult.articleId] && (
              <p className="mt-1 text-green-800 font-medium">
                Publicado: <a href={`/artigos/${publishResults[demoResult.articleId]}`} className="underline" target="_blank" rel="noopener">{publishResults[demoResult.articleId]}</a>
              </p>
            )}
          </div>
        )}
      </section>

      {/* Drafts */}
      <section className="border border-gray-200 rounded-xl p-5">
        <div className="flex items-center justify-between mb-3">
          <h2 className="font-semibold text-gray-800">Drafts pendentes</h2>
          <button
            onClick={loadDrafts}
            disabled={draftsLoading}
            className="text-xs text-gray-500 hover:text-green-700 transition-colors disabled:opacity-50"
          >
            {draftsLoading ? 'Carregando...' : 'Atualizar'}
          </button>
        </div>
        {draftsError && <p className="text-red-600 text-sm">{draftsError}</p>}
        {!draftsLoading && drafts.length === 0 && !draftsError && (
          <p className="text-sm text-gray-400">Nenhum draft pendente.</p>
        )}
        {drafts.length > 0 && (
          <ul className="space-y-3">
            {drafts.map((draft) => {
              const result = publishResults[draft.id];
              return (
                <li key={draft.id} className="flex items-start gap-3 bg-gray-50 rounded-lg p-3 text-sm">
                  <div className="flex-1 min-w-0">
                    <p className="font-medium text-gray-900 truncate">{draft.title || draft.slug}</p>
                    <p className="text-xs text-gray-400 mt-0.5">
                      {draft.category ?? draft.articleType} &middot; {new Date(draft.createdAt).toLocaleString('pt-BR')}
                    </p>
                    {result && (
                      <p className="text-xs mt-1 text-green-700 font-medium">
                        {result.startsWith('Erro') ? (
                          <span className="text-red-600">{result}</span>
                        ) : (
                          <a href={`/artigos/${result}`} className="underline" target="_blank" rel="noopener">
                            Publicado: {result}
                          </a>
                        )}
                      </p>
                    )}
                  </div>
                  {!result && (
                    <button
                      onClick={() => publishArticle(draft.id)}
                      disabled={publishingId === draft.id}
                      className="shrink-0 bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white font-medium px-3 py-1.5 rounded-lg text-xs transition-colors"
                    >
                      {publishingId === draft.id ? '...' : 'Publicar'}
                    </button>
                  )}
                </li>
              );
            })}
          </ul>
        )}
      </section>
    </div>
  );
}
