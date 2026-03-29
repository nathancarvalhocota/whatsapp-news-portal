import type { Metadata } from 'next';
import './globals.css';
import { Geist } from 'next/font/google';
import { cn } from '@/lib/utils';
import { Separator } from '@/components/ui/separator';

const geist = Geist({ subsets: ['latin'], variable: '--font-sans' });

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
    <html lang="pt-BR" className={cn('font-sans', geist.variable)}>
      <body className="min-h-screen flex flex-col bg-white text-gray-900 antialiased">
        {/* Header */}
        <header className="sticky top-0 z-50 bg-white/95 backdrop-blur-sm border-b border-gray-100 shadow-sm">
          <div className="mx-auto max-w-5xl px-4 sm:px-6 h-16 flex items-center gap-6">
            {/* Logo */}
            <a href="/" className="flex items-center gap-2.5 mr-auto shrink-0">
              <div className="w-8 h-8 rounded-lg bg-green-500 flex items-center justify-center flex-shrink-0">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  width="16"
                  height="16"
                  viewBox="0 0 24 24"
                  fill="white"
                  aria-hidden="true"
                >
                  <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347z" />
                  <path d="M12 0C5.373 0 0 5.373 0 12c0 2.136.563 4.14 1.537 5.875L0 24l6.318-1.508A11.94 11.94 0 0012 24c6.627 0 12-5.373 12-12S18.627 0 12 0zm0 21.818a9.802 9.802 0 01-5.028-1.381l-.36-.215-3.728.888.936-3.619-.237-.374A9.79 9.79 0 012.182 12C2.182 6.573 6.573 2.182 12 2.182S21.818 6.573 21.818 12 17.427 21.818 12 21.818z" />
                </svg>
              </div>
              <div>
                <span className="text-lg font-bold text-gray-900 leading-none block">
                  WhatsApp News
                </span>
                <span className="hidden sm:block text-xs text-gray-400 leading-none mt-0.5">
                  Portal de Notícias
                </span>
              </div>
            </a>

            {/* Nav */}
            <nav className="flex items-center gap-1 text-sm font-medium">
              <a
                href="/categorias/oficial"
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-gray-600 hover:text-green-700 hover:bg-green-50 transition-colors"
              >
                <span className="w-1.5 h-1.5 rounded-full bg-green-500 flex-shrink-0" />
                Oficial
              </a>
              <a
                href="/categorias/beta"
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-gray-600 hover:text-amber-700 hover:bg-amber-50 transition-colors"
              >
                <span className="w-1.5 h-1.5 rounded-full bg-amber-400 flex-shrink-0" />
                Beta
              </a>
            </nav>
          </div>
        </header>

        {/* Main content */}
        <main className="flex-1 mx-auto w-full max-w-5xl px-4 sm:px-6 py-10">{children}</main>

        {/* Footer */}
        <footer className="mt-20 border-t border-gray-100 bg-gray-50">
          <div className="mx-auto max-w-5xl px-4 sm:px-6 py-10">
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-6">
              {/* Brand */}
              <div>
                <a href="/" className="flex items-center gap-2 mb-2">
                  <div className="w-6 h-6 rounded bg-green-500 flex items-center justify-center flex-shrink-0">
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      width="12"
                      height="12"
                      viewBox="0 0 24 24"
                      fill="white"
                      aria-hidden="true"
                    >
                      <path d="M12 0C5.373 0 0 5.373 0 12c0 2.136.563 4.14 1.537 5.875L0 24l6.318-1.508A11.94 11.94 0 0012 24c6.627 0 12-5.373 12-12S18.627 0 12 0zm0 21.818a9.802 9.802 0 01-5.028-1.381l-.36-.215-3.728.888.936-3.619-.237-.374A9.79 9.79 0 012.182 12C2.182 6.573 6.573 2.182 12 2.182S21.818 6.573 21.818 12 17.427 21.818 12 21.818z" />
                    </svg>
                  </div>
                  <span className="font-bold text-gray-900 text-sm">
                    WhatsApp News
                  </span>
                </a>
                <p className="text-xs text-gray-400 max-w-xs leading-relaxed">
                  Notícias sobre o ecossistema WhatsApp geradas automaticamente
                  com inteligência artificial.
                </p>
              </div>

              {/* Links */}
              <div className="flex flex-col gap-2 text-sm">
                <a
                  href="/categorias/oficial"
                  className="text-gray-500 hover:text-green-700 transition-colors"
                >
                  Notícias Oficiais
                </a>
                <a
                  href="/categorias/beta"
                  className="text-gray-500 hover:text-amber-700 transition-colors"
                >
                  Recursos Beta
                </a>
              </div>
            </div>

            <Separator className="my-6" />

            <div className="flex flex-col sm:flex-row items-center justify-between gap-2 text-xs text-gray-400">
              <span>
                © {new Date().getFullYear()} WhatsApp News. Conteúdo gerado por IA.
              </span>
              <span className="flex items-center gap-1.5">
                Desenvolvido para o hackathon{' '}
                <a
                  href="https://umbler.com"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="font-semibold text-gray-600 hover:text-gray-800 transition-colors"
                >
                  Umbler
                </a>
              </span>
            </div>
          </div>
        </footer>
      </body>
    </html>
  );
}
