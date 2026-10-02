"use client";

import { isStaff, resolveHomeRoute } from "@/src/shared/auth/roles";
import { useIsHydrated } from "@/src/shared/lib/use-is-hydrated";
import { routes } from "@/src/shared/routes";
import {
  sessionSelectors,
  useSessionStore,
} from "@/src/shared/stores/session-store";
import { SiteHeader } from "@/src/shared/ui/site-header";
import { KeyRound, LayoutDashboard, ShieldCheck, UserRound } from "lucide-react";
import Link from "next/link";

const primaryLinkClass =
  "inline-flex h-10 items-center justify-center rounded-md bg-white px-5 text-sm font-medium text-slate-900 transition-colors hover:bg-slate-200";
const secondaryLinkClass =
  "inline-flex h-10 items-center justify-center rounded-md border border-slate-700 px-5 text-sm font-medium text-slate-200 transition-colors hover:bg-slate-900";

const features = [
  {
    icon: KeyRound,
    title: "Единый вход",
    description:
      "Authorization Code + PKCE через AuthService: одна учётная запись для всех сервисов платформы.",
    tone: "bg-sky-50 text-sky-700",
  },
  {
    icon: UserRound,
    title: "Личный кабинет",
    description:
      "Профиль, роли и данные аккаунта доступны по защищённому API с Bearer-токеном.",
    tone: "bg-emerald-50 text-emerald-700",
  },
  {
    icon: ShieldCheck,
    title: "Админ-панель",
    description:
      "Для Admin и Moderator: статистика аккаунтов, поиск и управление пользователями.",
    tone: "bg-rose-50 text-rose-700",
  },
] as const;

const steps = [
  {
    title: "Вход",
    description: "Email и пароль на защищённой странице входа.",
  },
  {
    title: "Роль",
    description: "AuthService выдаёт токен с ролями и permissions.",
  },
  {
    title: "Свой интерфейс",
    description: "Игрок видит кабинет, персонал — админ-панель.",
  },
] as const;

export default function Home() {
  const hydrated = useIsHydrated();
  const isAuthenticated = useSessionStore(sessionSelectors.isAuthenticated);
  const user = useSessionStore(sessionSelectors.user);

  const userRoles = user?.roles ?? [];
  const staff = isStaff(userRoles);
  // До гидратации состояние сессии неизвестно — рендерим «гостевой» вариант,
  // как на сервере, чтобы не было расхождения разметки.
  const showAuthenticated = hydrated && isAuthenticated;

  return (
    <>
      <SiteHeader />

      <main className="flex-1">
        <section className="border-b border-slate-200 bg-slate-950">
          <div className="mx-auto flex w-full max-w-5xl flex-col items-center px-6 py-20 text-center">
            <p className="text-xs font-semibold tracking-widest text-slate-400 uppercase">
              Игровая платформа v2
            </p>
            <h1 className="mt-3 text-4xl font-bold tracking-tight text-white sm:text-5xl">
              GameServer
            </h1>
            <p className="mt-4 max-w-2xl text-base text-slate-300">
              Единый вход в личный кабинет игрока и админ-панель. Регистрация
              открыта для всех, интерфейс подстраивается под вашу роль.
            </p>

            <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
              {showAuthenticated ? (
                <>
                  <Link
                    href={resolveHomeRoute(userRoles)}
                    className="inline-flex h-10 items-center justify-center rounded-md bg-white px-5 text-sm font-medium text-slate-900 transition-colors hover:bg-slate-200"
                  >
                    {staff ? "Открыть админ-панель" : "Открыть личный кабинет"}
                  </Link>
                  <Link
                    href={routes.profile}
                    className="inline-flex h-10 items-center justify-center rounded-md border border-slate-700 px-5 text-sm font-medium text-slate-200 transition-colors hover:bg-slate-900"
                  >
                    Мой профиль
                  </Link>
                </>
              ) : (
                <>
                  <Link href={routes.login} className={primaryLinkClass}>
                    Войти
                  </Link>
                  <Link href={routes.register} className={secondaryLinkClass}>
                    Создать аккаунт
                  </Link>
                </>
              )}
            </div>
          </div>
        </section>

        <section className="mx-auto w-full max-w-5xl px-6 py-14">
          <div className="grid gap-4 sm:grid-cols-3">
            {steps.map((step, index) => (
              <div
                key={step.title}
                className="rounded-lg border border-slate-200 bg-white p-5"
              >
                <span className="inline-flex h-7 w-7 items-center justify-center rounded-full bg-slate-900 text-xs font-semibold text-white">
                  {index + 1}
                </span>
                <h2 className="mt-3 text-sm font-semibold text-slate-900">
                  {step.title}
                </h2>
                <p className="mt-1 text-sm text-slate-600">
                  {step.description}
                </p>
              </div>
            ))}
          </div>

          <div className="mt-4 grid gap-4 sm:grid-cols-3">
            {features.map((feature) => (
              <section
                key={feature.title}
                className="rounded-lg border border-slate-200 bg-white p-5"
              >
                <span
                  aria-hidden="true"
                  className={`inline-flex rounded-md p-2 ${feature.tone}`}
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

          <div className="mt-10 flex items-center justify-center gap-2 text-xs text-slate-400">
            <LayoutDashboard className="h-3.5 w-3.5" />
            OIDC · JWT · PostgreSQL · nginx
          </div>
        </section>
      </main>
    </>
  );
}
