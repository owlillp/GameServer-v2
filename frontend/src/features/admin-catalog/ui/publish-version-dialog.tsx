"use client";

import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Textarea } from "@/src/shared/ui/textarea";
import { Upload, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";

type Props = {
  open: boolean;
  isPending: boolean;
  error: unknown;
  onPublish: (note: string | null) => Promise<unknown>;
  onClose: () => void;
};

export function PublishVersionDialog({
  open,
  isPending,
  error,
  onPublish,
  onClose,
}: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [note, setNote] = useState("");

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (open && !dialog.open) {
      dialog.showModal();
    } else if (!open && dialog.open) {
      dialog.close();
    }
  }, [open]);

  const onConfirm = async () => {
    try {
      await onPublish(note.trim() || null);
      setNote("");
      onClose();
    } catch {
      // Ошибка отображается через ApiErrorList.
    }
  };

  return (
    <dialog
      ref={dialogRef}
      onClose={onClose}
      aria-labelledby="publish-version-title"
      className="m-auto w-[min(30rem,calc(100vw-2rem))] rounded-xl border border-slate-200 bg-white p-0 shadow-xl backdrop:bg-slate-900/40"
    >
      <div className="flex items-start gap-3 border-b border-slate-100 p-5">
        <span className="inline-flex h-9 w-9 items-center justify-center rounded-lg bg-slate-900 text-white">
          <Upload className="h-4 w-4" aria-hidden="true" />
        </span>
        <div className="flex-1">
          <h2
            id="publish-version-title"
            className="text-base font-semibold text-slate-900"
          >
            Опубликовать версию каталога
          </h2>
          <p className="mt-0.5 text-xs text-slate-500">
            Все текущие черновики и изменения станут доступны клиентам.
            Предыдущая версия сохранится как неизменяемый снапшот.
          </p>
        </div>
        <button
          type="button"
          onClick={onClose}
          aria-label="Закрыть"
          className="inline-flex h-7 w-7 items-center justify-center rounded-md text-slate-400 hover:bg-slate-100 hover:text-slate-700"
        >
          <X className="h-4 w-4" aria-hidden="true" />
        </button>
      </div>

      <div className="space-y-3 p-5">
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Заметка к релизу (необязательно)
          </span>
          <Textarea
            value={note}
            onChange={(event) => setNote(event.target.value)}
            rows={2}
            maxLength={512}
            placeholder="Сезон 2: добавлены машины и тюнинг"
          />
        </label>

        {error ? (
          <ApiErrorList error={error} fallback="Не удалось опубликовать версию" />
        ) : null}
      </div>

      <div className="flex justify-end gap-3 border-t border-slate-100 p-4">
        <Button variant="ghost" onClick={onClose} disabled={isPending}>
          Отмена
        </Button>
        <Button onClick={() => void onConfirm()} disabled={isPending}>
          {isPending ? "Публикуем..." : "Опубликовать"}
        </Button>
      </div>
    </dialog>
  );
}
