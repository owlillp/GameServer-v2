"use client";

import type { Item } from "@/src/entities/items";
import { cn } from "@/src/shared/lib/utils";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { ConfirmDialog } from "@/src/shared/ui/confirm-dialog";
import { Input } from "@/src/shared/ui/input";
import { Spinner } from "@/src/shared/ui/spinner";
import { useState } from "react";
import { useDebounce } from "use-debounce";
import { useDeleteItem, useItemsList } from "../model/use-items";
import { ItemForm } from "./item-form";

const PAGE_SIZE = 10;

export function ItemsExplorer() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [showCreate, setShowCreate] = useState(false);
  const [editing, setEditing] = useState<Item | null>(null);
  const [deleting, setDeleting] = useState<Item | null>(null);
  const [debouncedSearch] = useDebounce(search, 300);

  const { items, totalCount, totalPages, isPending, isFetching, error } =
    useItemsList({ page, pageSize: PAGE_SIZE, search: debouncedSearch });

  const {
    deleteItem,
    isPending: isDeleting,
    error: deleteError,
  } = useDeleteItem();

  const from = totalCount === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const to = Math.min(page * PAGE_SIZE, totalCount);

  const confirmDelete = async () => {
    if (!deleting) return;

    try {
      await deleteItem(deleting.id);
      setDeleting(null);
    } catch {
      // Ошибка уже в deleteError — покажем ниже.
    }
  };

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-end justify-between gap-3">
        <div className="w-full max-w-xs">
          <label
            htmlFor="item-search"
            className="mb-1 block text-sm font-medium text-slate-700"
          >
            Поиск
          </label>
          <Input
            id="item-search"
            name="search"
            type="search"
            enterKeyHint="search"
            placeholder="Название"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
          />
        </div>

        <div className="flex items-center gap-3">
          <p role="status" className="text-xs text-slate-500">
            Найдено: {totalCount}
          </p>
          <Button
            onClick={() => {
              setEditing(null);
              setShowCreate((value) => !value);
            }}
          >
            {showCreate ? "Свернуть форму" : "Создать запись"}
          </Button>
        </div>
      </div>

      {showCreate && (
        <div className="mb-6">
          <ItemForm onSaved={() => setShowCreate(false)} />
        </div>
      )}

      {editing && (
        <div className="mb-6">
          <ItemForm
            item={editing}
            onSaved={() => setEditing(null)}
            onCancel={() => setEditing(null)}
          />
        </div>
      )}

      {error ? (
        <ApiErrorList error={error} fallback="Не удалось загрузить записи" />
      ) : isPending ? (
        <Spinner label="Загружаем записи..." />
      ) : items.length === 0 ? (
        <p className="rounded-lg border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-500">
          {debouncedSearch
            ? "Ничего не найдено."
            : "Записей пока нет — создай первую."}
        </p>
      ) : (
        <div
          aria-busy={isFetching}
          className={cn("transition-opacity", isFetching && "opacity-60")}
        >
          <ul className="space-y-3">
            {items.map((item) => (
              <li
                key={item.id}
                className="rounded-lg border border-slate-200 bg-white p-4"
              >
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div className="min-w-0">
                    <h3 className="text-sm font-semibold text-slate-900">
                      {item.name}
                    </h3>
                    {item.description && (
                      <p className="mt-1 text-sm text-slate-600">
                        {item.description}
                      </p>
                    )}
                    <p className="mt-2 text-xs text-slate-500">
                      Владелец:{" "}
                      <span className="font-mono">{item.ownerId}</span> ·
                      создано {formatDate(item.createdAt)}
                    </p>
                  </div>

                  <div className="flex shrink-0 items-center gap-1">
                    <Button
                      variant="ghost"
                      onClick={() => {
                        setShowCreate(false);
                        setEditing(item);
                      }}
                    >
                      Изменить
                    </Button>
                    <Button variant="ghost" onClick={() => setDeleting(item)}>
                      Удалить
                    </Button>
                  </div>
                </div>
              </li>
            ))}
          </ul>

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
              Показано {from}–{to} из {totalCount} · страница {page} из{" "}
              {totalPages}
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

      <ApiErrorList
        error={deleteError}
        fallback="Не удалось удалить запись"
        className="mt-4"
      />

      <ConfirmDialog
        open={deleting !== null}
        title="Удалить запись?"
        description={
          deleting
            ? `«${deleting.name}» будет удалена без возможности восстановления.`
            : undefined
        }
        confirmLabel="Удалить"
        isPending={isDeleting}
        onConfirm={() => void confirmDelete()}
        onCancel={() => setDeleting(null)}
      />
    </div>
  );
}

function formatDate(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;

  return date.toLocaleDateString("ru-RU", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  });
}
