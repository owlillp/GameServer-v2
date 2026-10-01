import { routes } from "@/src/shared/routes";
import { SiteHeader } from "@/src/shared/ui/site-header";
import { Database, KeyRound, Layers, Server } from "lucide-react";
import Link from "next/link";

const primaryLinkClass =
  "inline-flex h-10 items-center justify-center rounded-md bg-slate-900 px-5 text-sm font-medium text-white transition-colors hover:bg-slate-800";

const features = [
  {
    icon: Server,
    title: "Backend",
    description:
      ".NET 10: Domain / Core (CQRS) / Contracts / Infrastructure.Postgres / Web с health-check и Scalar.",
  },
  {
    icon: KeyRound,
    title: "Авторизация",
    description:
      "JWT-валидация токенов AuthService, permissions по ролям, UserScopedData в handlers.",
  },
  {
    icon: Database,
    title: "PostgreSQL + EF Core",
    description:
      "Собственная схема, миграции, отдельный migrator-контейнер и интеграционные тесты на Testcontainers.",
  },
  {
    icon: Layers,
    title: "Infra",
    description:
      "Docker Compose, nginx с HTTPS и Next.js frontend с React Query и envelope-обработкой ошибок.",
  },
] as const;

export default function Home() {
  return (
    <>
      <SiteHeader />

      <main className="mx-auto flex w-full max-w-5xl flex-1 flex-col items-center px-6 py-16">
        <div className="max-w-2xl text-center">
          <p className="text-xs font-semibold tracking-widest text-slate-500 uppercase">
            Шаблонный сервис
          </p>
          <h1 className="mt-3 text-4xl font-bold tracking-tight text-slate-900">
            Template Service
          </h1>
          <p className="mt-4 text-base text-slate-600">
            Копируй структуру, переименовывай проекты и получай новый сервис с
            бекендом, фронтендом, nginx и docker без ручной настройки.
          </p>

          <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
            <Link href={routes.items} className={primaryLinkClass}>
              Открыть пример CRUD
            </Link>
          </div>
        </div>

        <div className="mt-16 grid w-full gap-4 sm:grid-cols-2">
          {features.map((feature) => (
            <section
              key={feature.title}
              className="rounded-lg border border-slate-200 bg-white p-5"
            >
              <span
                aria-hidden="true"
                className="inline-flex rounded-md bg-slate-100 p-2 text-slate-700"
              >
                <feature.icon className="h-5 w-5" />
              </span>
              <h2 className="mt-3 text-sm font-semibold text-slate-900">
                {feature.title}
              </h2>
              <p className="mt-1 text-sm text-slate-600">
                {feature.description}
              </p>
            </section>
          ))}
        </div>
      </main>
    </>
  );
}
