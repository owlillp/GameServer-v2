import { AdminStatsCards } from "@/src/features/admin-stats";
import { routes } from "@/src/shared/routes";
import Link from "next/link";

export default function AdminOverviewPage() {
  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-2xl font-semibold text-slate-900">Админ-панель</h1>
        <p className="text-sm text-slate-600">
          Обзор аккаунтов AuthService. Доступен ролям Admin и Moderator.
        </p>
      </header>

      <AdminStatsCards />

      <section className="rounded-lg border border-slate-200 bg-white p-6">
        <h2 className="text-sm font-semibold text-slate-900">Пользователи</h2>
        <p className="mt-1 text-sm text-slate-600">
          Список аккаунтов с ролями, статусом блокировки и поиском.
        </p>
        <Link
          href={routes.adminUsers}
          className="mt-4 inline-flex h-9 items-center justify-center rounded-md bg-slate-900 px-4 text-sm font-medium text-white transition-colors hover:bg-slate-800"
        >
          Открыть список
        </Link>
      </section>
    </div>
  );
}
