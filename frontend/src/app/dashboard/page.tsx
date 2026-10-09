"use client";

import { isStaff, resolveHomeRoute } from "@/src/shared/auth/roles";
import { RequireAuth } from "@/src/shared/auth/require-auth";
import { routes } from "@/src/shared/routes";
import {
  sessionSelectors,
  useSessionStore,
} from "@/src/shared/stores/session-store";
import { RoleBadge } from "@/src/shared/ui/role-badge";
import { SiteHeader } from "@/src/shared/ui/site-header";
import { KeyRound, Package, ShieldCheck, UserRound } from "lucide-react";
import Link from "next/link";

export default function DashboardPage() {
  const user = useSessionStore(sessionSelectors.user);
  const userRoles = user?.roles ?? [];
  const staff = isStaff(userRoles);
  const displayName = user?.name ?? user?.email ?? user?.sub ?? "игрок";

  return (
    <RequireAuth>
      <SiteHeader />

      <main className="mx-auto w-full max-w-3xl flex-1 space-y-6 px-6 py-10">
        <header className="space-y-1">
          <h1 className="text-2xl font-semibold text-slate-900">
            Личный кабинет
          </h1>
          <p className="text-sm text-slate-600">
            Вы вошли как <span className="font-medium">{displayName}</span>.
          </p>
        </header>

        <section className="rounded-lg border border-slate-200 bg-white p-6">
          <h2 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
            Аккаунт
          </h2>
          <dl className="mt-3 space-y-3 text-sm">
            <div className="flex flex-wrap justify-between gap-2">
              <dt className="text-slate-500">ID</dt>
              <dd className="font-mono text-xs break-all text-slate-900">
                {user?.sub ?? "—"}
              </dd>
            </div>
            <div className="flex flex-wrap justify-between gap-2">
              <dt className="text-slate-500">Email</dt>
              <dd className="font-medium text-slate-900">
                {user?.email ?? "—"}
              </dd>
            </div>
            <div className="flex flex-wrap justify-between gap-2">
              <dt className="text-slate-500">Имя</dt>
              <dd className="font-medium text-slate-900">
                {user?.name ?? "—"}
              </dd>
            </div>
            <div className="flex flex-wrap items-center justify-between gap-2">
              <dt className="text-slate-500">Роли из токена</dt>
              <dd className="flex flex-wrap gap-1">
                {userRoles.length > 0 ? (
                  userRoles.map((role) => <RoleBadge key={role} role={role} />)
                ) : (
                  <span className="text-slate-400">—</span>
                )}
              </dd>
            </div>
          </dl>
        </section>

        <section className="grid gap-4 sm:grid-cols-2">
          <div className="rounded-lg border border-slate-200 bg-white p-5">
            <span
              aria-hidden="true"
              className="inline-flex rounded-md bg-emerald-50 p-2 text-emerald-700"
            >
              <UserRound className="h-5 w-5" />
            </span>
            <h2 className="mt-3 text-sm font-semibold text-slate-900">
              Профиль игрока
            </h2>
            <p className="mt-1 text-sm text-slate-600">
              Контактные данные и публичная информация аккаунта.
            </p>
            <Link
              href={routes.profile}
              className="mt-4 inline-flex h-9 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100"
            >
              Открыть профиль
            </Link>
          </div>

          <div className="rounded-lg border border-slate-200 bg-white p-5">
            <span
              aria-hidden="true"
              className="inline-flex rounded-md bg-sky-50 p-2 text-sky-700"
            >
              <Package className="h-5 w-5" />
            </span>
            <h2 className="mt-3 text-sm font-semibold text-slate-900">
              Инвентарь
            </h2>
            <p className="mt-1 text-sm text-slate-600">
              Предметы во владении, аутфит и гараж с тюнингом.
            </p>
            <Link
              href={routes.inventory}
              className="mt-4 inline-flex h-9 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100"
            >
              Открыть инвентарь
            </Link>
          </div>

          {staff ? (
            <div className="rounded-lg border border-slate-200 bg-white p-5">
              <span
                aria-hidden="true"
                className="inline-flex rounded-md bg-rose-50 p-2 text-rose-700"
              >
                <ShieldCheck className="h-5 w-5" />
              </span>
              <h2 className="mt-3 text-sm font-semibold text-slate-900">
                Админ-панель
              </h2>
              <p className="mt-1 text-sm text-slate-600">
                Статистика аккаунтов и управление пользователями платформы.
              </p>
              <Link
                href={resolveHomeRoute(userRoles)}
                className="mt-4 inline-flex h-9 items-center justify-center rounded-md bg-slate-900 px-4 text-sm font-medium text-white transition-colors hover:bg-slate-800"
              >
                Открыть админ-панель
              </Link>
            </div>
          ) : (
            <div className="rounded-lg border border-slate-200 bg-white p-5">
              <span
                aria-hidden="true"
                className="inline-flex rounded-md bg-sky-50 p-2 text-sky-700"
              >
                <KeyRound className="h-5 w-5" />
              </span>
              <h2 className="mt-3 text-sm font-semibold text-slate-900">
                Единый вход
              </h2>
              <p className="mt-1 text-sm text-slate-600">
                Токен доступа обновляется автоматически и действует во всех
                сервисах платформы.
              </p>
              <span className="mt-4 inline-flex h-9 items-center rounded-md bg-slate-100 px-4 text-xs font-medium text-slate-500">
                OIDC (Bearer)
              </span>
            </div>
          )}
        </section>
      </main>
    </RequireAuth>
  );
}
