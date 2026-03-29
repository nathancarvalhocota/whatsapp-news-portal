import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  metadataBase: new URL(
    process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000',
  ),
  title: {
    default: 'WhatsApp News — Portal de Notícias',
    template: '%s | WhatsApp News',
  },
  description:
    'Portal de notícias em português sobre o ecossistema WhatsApp: atualizações oficiais, novidades beta e dicas para usuários e empresas.',
  openGraph: {
    type: 'website',
    locale: 'pt_BR',
    siteName: 'WhatsApp News',
  },
  robots: {
    index: true,
    follow: true,
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="pt-BR">
      <body className="min-h-screen bg-white text-gray-900 antialiased">
        <header className="border-b border-gray-200">
          <div className="mx-auto max-w-4xl px-4 py-4 flex flex-wrap items-center gap-4">
            <a href="/" className="text-xl font-bold text-green-600 mr-auto">
              WhatsApp News
            </a>
            <nav className="flex items-center gap-4 text-sm">
              <a
                href="/categorias/oficial"
                className="text-gray-600 hover:text-green-700 transition-colors"
              >
                Oficial
              </a>
              <a
                href="/categorias/beta"
                className="text-gray-600 hover:text-amber-700 transition-colors"
              >
                Beta
              </a>
            </nav>
          </div>
        </header>
        <main className="mx-auto max-w-4xl px-4 py-8">{children}</main>
        <footer className="border-t border-gray-200 mt-16">
          <div className="mx-auto max-w-4xl px-4 py-6 text-sm text-gray-500 text-center">
            © {new Date().getFullYear()} WhatsApp News — Umbler
          </div>
        </footer>
      </body>
    </html>
  );
}
