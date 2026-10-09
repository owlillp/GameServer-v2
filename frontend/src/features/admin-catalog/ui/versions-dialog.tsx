"use client";

import type {
  CatalogVersion,
  CatalogVersionDetail,
  CatalogVersionItem,
} from "@/src/entities/catalog";
import { cn } from "@/src/shared/lib/utils";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Spinner } from "@/src/shared/ui/spinner";
import { Textarea } from "@/src/shared/ui/textarea";
import { History, X } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useCatalogVersionDetail } from "../model/use-catalog-version-detail";
import { KindBadge, RarityBadge } from "./catalog-badges";

type Props = {
  open: boolean;
  versions: CatalogVersion[];
  isLoading: boolean;
  loadError: unknown;
  isPending: boolean;
  error: unknown;
  onRollback: (targetNumber: number, note: string | null) => Promise<unknown>;
  onClose: () => void;
};

export function VersionsDialog({
  open,
  versions,
  isLoading,
  loadError,
  isPending,
  error,
  onRollback,
  onClose,
}: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [selected, setSelected] = useState<number | null>(null);
  const [note, setNote] = useState("");

  const currentNumber = useMemo(
    () =>
      versions.length > 0 ? Math.max(...versions.map((v) => v.number)) : null,
    [versions],
  );

  const effectiveNumber = selected ?? currentNumber;

  const {
    detail,
    isPending: detailLoading,
    error: detailError,
  } = useCatalogVersionDetail(effectiveNumber, open);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (open && !dialog.open) {
      dialog.showModal();
    } else if (!open && dialog.open) {
      dialog.close();
    }
  }, [open]);

  const close = () => {
    setSelected(null);
    setNote("");
    onClose();
  };

  const canRollback =
    effectiveNumber !== null && effectiveNumber !== currentNumber;

  const onConfirm = async () => {
    if (!canRollback || effectiveNumber === null) return;
    try {
      await onRollback(effectiveNumber, note.trim() || null);
      close();
    } catch {
      // Ошибка отображается через ApiErrorList.
    }
  };

  return (
    <dialog
      ref={dialogRef}
      onClose={close}
      aria-labelledby="versions-dialog-title"
      className="m-auto w-[min(60rem,calc(100vw-2rem))] rounded-xl border border-slate-200 bg-white p-0 shadow-xl backdrop:bg-slate-900/40"
    >
      <div className="flex items-start gap-3 border-b border-slate-100 p-5">
        <span className="inline-flex h-9 w-9 items-center justify-center rounded-lg bg-slate-900 text-white">
          <History className="h-4 w-4" aria-hidden="true" />
        </span>
        <div className="flex-1">
          <h2
            id="versions-dialog-title"
            className="text-base font-semibold text-slate-900"
          >
            Версии каталога
          </h2>
          <p className="mt-0.5 text-xs text-slate-500">
            Выберите версию, чтобы посмотреть её содержимое и статус. Откат
            создаёт новую версию-снапшот — история сохраняется.
          </p>
        </div>
        <button
          type="button"
          onClick={close}
          aria-label="Закрыть"
          className="inline-flex h-7 w-7 items-center justify-center rounded-md text-slate-400 hover:bg-slate-100 hover:text-slate-700"
        >
          <X className="h-4 w-4" aria-hidden="true" />
        </button>
      </div>

      <div className="grid gap-0 md:grid-cols-[16rem_1fr]">
        <div className="max-h-96 space-y-2 overflow-y-auto border-b border-slate-100 p-4 md:border-r md:border-b-0">
          {loadError ? (
            <ApiErrorList
              error={loadError}
              fallback="Не удалось загрузить версии"
            />
          ) : isLoading ? (
            <Spinner label="Загружаем версии..." />
          ) : versions.length === 0 ? (
            <p className="text-sm text-slate-500">
              Версий ещё нет — каталог не публиковался.
            </p>
          ) : (
            versions.map((version) => {
              const isSelected = version.number === effectiveNumber;

              return (
                <button
                  key={version.id}
                  type="button"
                  onClick={() => setSelected(version.number)}
                  className={cn(
                    "w-full rounded-lg border p-3 text-left transition-colors",
                    isSelected
                      ? "border-slate-900 ring-1 ring-slate-900"
                      : "border-slate-200 hover:border-slate-300",
                  )}
                >
                  <p className="flex items-center gap-2 text-sm font-medium text-slate-900">
                    v{version.number}
                    <VersionStatus status={version.status} />
                  </p>
                  <p className="mt-0.5 text-xs text-slate-500">
                    {version.itemCount} предметов ·{" "}
                    {formatDate(version.publishedAt)}
                  </p>
                </button>
              );
            })
          )}
        </div>

        <div className="max-h-96 overflow-y-auto p-4">
          <VersionContent
            detail={detail}
            isLoading={detailLoading}
            error={detailError}
          />
        </div>
      </div>

      <div className="space-y-3 border-t border-slate-100 p-5">
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Заметка к откату (необязательно)
          </span>
          <Textarea
            value={note}
            onChange={(event) => setNote(event.target.value)}
            rows={2}
            maxLength={512}
            placeholder="Откат из-за некорректных цен"
          />
        </label>

        {error ? (
          <ApiErrorList error={error} fallback="Не удалось выполнить откат" />
        ) : null}

        <div className="flex justify-end gap-3">
          <Button variant="ghost" onClick={close} disabled={isPending}>
            Закрыть
          </Button>
          <Button
            onClick={() => void onConfirm()}
            disabled={isPending || !canRollback}
          >
            {isPending
              ? "Откатываем..."
              : effectiveNumber !== null
                ? `Откатить к v${effectiveNumber}`
                : "Откатить"}
          </Button>
        </div>
      </div>
    </dialog>
  );
}

function VersionContent({
  detail,
  isLoading,
  error,
}: {
  detail: CatalogVersionDetail | null;
  isLoading: boolean;
  error: unknown;
}) {
  if (error) {
    return (
      <ApiErrorList error={error} fallback="Не удалось загрузить содержимое" />
    );
  }

  if (isLoading || !detail) {
    return <Spinner label="Загружаем содержимое..." />;
  }

  return (
    <div className="space-y-3">
      <header className="space-y-1 border-b border-slate-100 pb-3">
        <p className="flex items-center gap-2 text-sm font-semibold text-slate-900">
          Версия v{detail.number}
          <VersionStatus status={detail.status} />
        </p>
        <p className="text-xs text-slate-500">
          Опубликована {formatDate(detail.publishedAt)} · {detail.itemCount}{" "}
          предметов
        </p>
        {detail.note ? (
          <p className="text-xs text-slate-600">Заметка: {detail.note}</p>
        ) : null}
      </header>

      {detail.items.length === 0 ? (
        <p className="text-sm text-slate-500">В этой версии нет предметов.</p>
      ) : (
        <ul className="divide-y divide-slate-100 rounded-md border border-slate-200">
          {detail.items.map((item) => (
            <VersionItemRow key={item.itemId} item={item} />
          ))}
        </ul>
      )}
    </div>
  );
}

function VersionItemRow({ item }: { item: CatalogVersionItem }) {
  return (
    <li className="flex items-center justify-between gap-3 px-3 py-2.5">
      <div className="min-w-0">
        <p className="truncate text-sm font-medium text-slate-900">
          {item.displayName}
        </p>
        <p className="flex items-center gap-2 truncate font-mono text-[11px] text-slate-400">
          {item.code}
          {item.slot ? <span>· {item.slot}</span> : null}
          {item.modelCode ? <span>· {item.modelCode}</span> : null}
        </p>
      </div>
      <div className="flex shrink-0 items-center gap-2">
        <KindBadge kind={item.kind} />
        <RarityBadge rarity={item.rarity} />
      </div>
    </li>
  );
}

function VersionStatus({ status }: { status: string }) {
  const isCurrent = status === "CURRENT";

  return (
    <span
      className={cn(
        "rounded-full px-2 py-0.5 text-[11px] font-medium ring-1 ring-inset",
        isCurrent
          ? "bg-emerald-50 text-emerald-700 ring-emerald-200"
          : "bg-slate-100 text-slate-500 ring-slate-200",
      )}
    >
      {isCurrent ? "текущая" : "заменена"}
    </span>
  );
}

function formatDate(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;

  return date.toLocaleString("ru-RU", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}
