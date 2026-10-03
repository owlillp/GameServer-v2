"use client";

import type { AdminUser } from "@/src/entities/admin";
import { cn } from "@/src/shared/lib/utils";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Input } from "@/src/shared/ui/input";
import { RoleBadge } from "@/src/shared/ui/role-badge";
import { Spinner } from "@/src/shared/ui/spinner";
import { useDebounce } from "use-debounce";
import { useState, type ReactNode } from "react";
import { useAdminUsers } from "../model/use-admin-users";

const PAGE_SIZE = 20;

export function AdminUsersTable() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [debouncedSearch] = useDebounce(search, 300);

  const { users, totalCount, totalPages, isPending, isFetching, error } =
    useAdminUsers({ page, pageSize: PAGE_SIZE, search: debouncedSearch });

  const from = totalCount === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const to = Math.min(page * PAGE_SIZE, totalCount);

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-end justify-between gap-3">
        <div className="w-full max-w-xs">
          <label
            htmlFor="admin-user-search"
            className="mb-1 block text-sm font-medium text-slate-700"
          >
            Поиск
          </label>
          <Input
            id="admin-user-search"
            name="search"
            type="search"
            enterKeyHint="search"
            placeholder="Email или имя пользователя"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
          />
        </div>
        <p role="status" className="text-xs text-slate-500">
          Показано {from}–{to} из {totalCount}
        </p>
      </div>

      {error ? (
        <ApiErrorList
          error={error}
          fallback="Не удалось загрузить пользователей"
        />
      ) : isPending ? (
        <Spinner label="Загружаем пользователей..." />
      ) : users.length === 0 ? (
        <p className="rounded-lg border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-500">
          Ничего не найдено.
        </p>
      ) : (
        <div
          aria-busy={isFetching}
          className={cn(
            "@container transition-opacity",
            isFetching && "opacity-60",
          )}
        >
          <div className="max-h-[36rem] overflow-auto rounded-lg border border-slate-200 bg-white">
            <table className="w-full border-collapse text-left text-sm">
              <caption className="sr-only">
                Пользователи GameServer: имя, email, роли, статус блокировки и
                дата регистрации
              </caption>
              <thead className="hidden @2xl:table-header-group">
                <tr>
                  <HeaderCell>Пользователь</HeaderCell>
                  <HeaderCell>Email</HeaderCell>
                  <HeaderCell>Роли</HeaderCell>
                  <HeaderCell>Статус</HeaderCell>
                  <HeaderCell>Создан</HeaderCell>
                </tr>
              </thead>
              <tbody className="block @2xl:table-row-group">
                {users.map((user) => (
                  <UserRow key={user.id} user={user} />
                ))}
              </tbody>
            </table>
          </div>

          <nav
            aria-label="Постраничная навигация"
            className="mt-4 flex items-center justify-between gap-3"
          >
            <Button
              variant="ghost"
              disabled={page <= 1 || isFetching}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              Назад
            </Button>
            <span className="text-xs text-slate-500">
              Страница {page} из {totalPages}
            </span>
            <Button
              variant="ghost"
              disabled={page >= totalPages || isFetching}
              onClick={() => setPage((current) => current + 1)}
            >
              Вперёд
            </Button>
          </nav>
        </div>
      )}
    </div>
  );
}

function HeaderCell({ children }: { children: ReactNode }) {
  return (
    <th
      scope="col"
      className="sticky top-0 z-10 border-b border-slate-200 bg-slate-50 px-4 py-2.5 text-xs font-semibold tracking-wide text-slate-500 uppercase"
    >
      {children}
    </th>
  );
}

function UserRow({ user }: { user: AdminUser }) {
  const displayName = user.displayName ?? user.userName ?? "—";

  return (
    <tr className="block border-b border-slate-100 last:border-b-0 @2xl:table-row">
      <th
        scope="row"
        className="block bg-slate-50/60 px-4 py-2 text-left font-medium text-slate-900 @2xl:bg-transparent @2xl:px-4 @2xl:py-3 @2xl:font-normal"
      >
        <CellLabel>Пользователь</CellLabel>
        {displayName}
      </th>
      <td className="block px-4 py-1 @2xl:table-cell @2xl:px-4 @2xl:py-3">
        <CellLabel>Email</CellLabel>
        <span className="text-slate-700">{user.email ?? "—"}</span>
      </td>
      <td className="block px-4 py-1 @2xl:table-cell @2xl:px-4 @2xl:py-3">
        <CellLabel>Роли</CellLabel>
        <span className="flex flex-wrap gap-1">
          {user.roles.length > 0 ? (
            user.roles.map((role) => <RoleBadge key={role} role={role} />)
          ) : (
            <span className="text-slate-400">—</span>
          )}
        </span>
      </td>
      <td className="block px-4 py-1 @2xl:table-cell @2xl:px-4 @2xl:py-3">
        <CellLabel>Статус</CellLabel>
        <span
          className={cn(
            "text-xs font-medium",
            user.isLockedOut ? "text-rose-600" : "text-emerald-600",
          )}
        >
          {user.isLockedOut ? "Заблокирован" : "Активен"}
        </span>
      </td>
      <td className="block px-4 py-2 @2xl:table-cell @2xl:px-4 @2xl:py-3">
        <CellLabel>Создан</CellLabel>
        <span className="text-slate-500">{formatDate(user.createdAt)}</span>
      </td>
    </tr>
  );
}

// На узких контейнерах таблица превращается в карточки — подписи колонок
// приходят из этих sr-видимых лейблов.
function CellLabel({ children }: { children: ReactNode }) {
  return (
    <span className="mr-1 font-medium text-slate-500 @2xl:hidden">
      {children}:
    </span>
  );
}

function formatDate(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;

  return date.toLocaleString("ru-RU", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}
