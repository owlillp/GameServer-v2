"use client";

import {
  ITEM_KINDS,
  ITEM_RARITIES,
  type AvailabilityWindow,
  type CreateItemRequest,
  type ItemDetail,
  type ItemKind,
  type ItemPrice,
  type ItemRarity,
  type ItemStat,
  type UpdateItemRequest,
} from "@/src/entities/catalog";
import { currenciesQueryOptions } from "@/src/entities/wallet";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Input } from "@/src/shared/ui/input";
import { Select } from "@/src/shared/ui/select";
import { Textarea } from "@/src/shared/ui/textarea";
import { useQuery } from "@tanstack/react-query";
import { Minus, Plus, RotateCcw, Save, Trash2 } from "lucide-react";
import { useMemo, useState, type FormEvent } from "react";
import { KIND_META, RARITY_LABEL, StatusBadge } from "./catalog-badges";
import type { SaveItemInput } from "../model/use-save-item";

type Props = {
  item: ItemDetail | null;
  isPending: boolean;
  error: unknown;
  onSave: (input: SaveItemInput) => Promise<ItemDetail>;
  onArchive?: (itemId: string) => Promise<unknown>;
  onReactivate?: (itemId: string) => Promise<unknown>;
  onCancel: () => void;
  reset: () => void;
};

type StatDraft = { key: string; value: string };
type PriceDraft = { currencyCode: string; amount: string; isActive: boolean };
type AvailabilityDraft = {
  startsAt: string;
  endsAt: string;
  channel: string;
};

export function ItemForm({
  item,
  isPending,
  error,
  onSave,
  onArchive,
  onReactivate,
  onCancel,
  reset,
}: Props) {
  const isCreate = item === null;

  const [code, setCode] = useState(item?.code ?? "");
  const [kind, setKind] = useState<ItemKind>(item?.kind ?? "CLOTHING");
  const [rarity, setRarity] = useState<ItemRarity>(item?.rarity ?? "COMMON");
  const [displayName, setDisplayName] = useState(item?.displayName ?? "");
  const [description, setDescription] = useState(item?.description ?? "");
  const [modelCode, setModelCode] = useState(item?.modelCode ?? "");
  const [slot, setSlot] = useState(item?.slot ?? "");
  const [tagsText, setTagsText] = useState((item?.tags ?? []).join(", "));
  const [fitTagsText, setFitTagsText] = useState(
    (item?.fitTags ?? []).join(", "),
  );
  const [stats, setStats] = useState<StatDraft[]>(
    (item?.stats ?? []).map((stat) => ({
      key: stat.key,
      value: String(stat.value),
    })),
  );
  const [prices, setPrices] = useState<PriceDraft[]>(
    (item?.prices ?? []).map((price) => ({
      currencyCode: price.currencyCode,
      amount: String(price.amount),
      isActive: price.isActive,
    })),
  );
  const [availability, setAvailability] = useState<AvailabilityDraft[]>(
    (item?.availability ?? []).map((window) => ({
      startsAt: toLocalInput(window.startsAt),
      endsAt: toLocalInput(window.endsAt),
      channel: window.channel ?? "",
    })),
  );
  const [archiveOpen, setArchiveOpen] = useState(false);

  const { data: currencies } = useQuery(currenciesQueryOptions());

  const fitTags = useMemo(() => splitList(fitTagsText), [fitTagsText]);
  const needsModelCode = kind === "CAR" && modelCode.trim().length === 0;
  const needsFitTags = kind === "CAR_PART" && fitTags.length === 0;

  const isValid =
    displayName.trim().length > 0 &&
    (isCreate ? code.trim().length > 0 : true) &&
    !needsModelCode &&
    !needsFitTags;

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    reset();
    if (!isValid) return;

    const payload = {
      displayName: displayName.trim(),
      description: description.trim() || null,
      rarity,
      modelCode: modelCode.trim() || null,
      slot: slot.trim() || null,
      tags: splitList(tagsText),
      fitTags,
      stats: toStats(stats),
      prices: toPrices(prices),
      availability: toAvailability(availability),
    };

    try {
      if (item) {
        const request: UpdateItemRequest = payload;
        await onSave({ mode: "update", itemId: item.id, request });
      } else {
        const request: CreateItemRequest = {
          ...payload,
          code: code.trim().toLowerCase(),
          kind,
        };
        await onSave({ mode: "create", request });
      }
    } catch {
      // Ошибка отображается через ApiErrorList.
    }
  };

  const onArchiveClick = async () => {
    if (!item || !onArchive) return;
    reset();
    try {
      await onArchive(item.id);
      setArchiveOpen(false);
      onCancel();
    } catch {
      // Ошибка отображается через ApiErrorList.
    }
  };

  const onReactivateClick = async () => {
    if (!item || !onReactivate) return;
    reset();
    try {
      await onReactivate(item.id);
    } catch {
      // Ошибка отображается через ApiErrorList.
    }
  };

  return (
    <form
      onSubmit={onSubmit}
      className="flex flex-col gap-5 rounded-xl border border-slate-200 bg-white p-5 shadow-sm"
    >
      <header className="flex flex-wrap items-start justify-between gap-3 border-b border-slate-100 pb-4">
        <div className="space-y-1">
          <h2 className="text-base font-semibold text-slate-900">
            {isCreate ? "Новый предмет" : item.displayName}
          </h2>
          <p className="text-xs text-slate-500">
            {isCreate
              ? "Предмет создаётся в статусе «Черновик» и попадает в каталог после публикации версии."
              : `Код: ${item.code}`}
          </p>
        </div>
        {item && (
          <div className="flex items-center gap-2">
            <StatusBadge status={item.status} />
            {item.lastPublishedVersion != null && (
              <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-medium text-slate-500">
                v{item.lastPublishedVersion}
              </span>
            )}
          </div>
        )}
      </header>

      <Section title="Основное">
        <Field label="Код" hint="латиница, уникальный">
          <Input
            value={code}
            onChange={(event) => setCode(event.target.value)}
            placeholder="weapon_ak47"
            disabled={!isCreate}
            required={isCreate}
          />
        </Field>

        <Field label="Тип">
          <Select
            value={kind}
            onChange={(event) => setKind(event.target.value as ItemKind)}
            disabled={!isCreate}
          >
            {ITEM_KINDS.map((value) => (
              <option key={value} value={value}>
                {KIND_META[value].label} ({value})
              </option>
            ))}
          </Select>
        </Field>

        <Field label="Название">
          <Input
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
            placeholder="AK-47"
            required
          />
        </Field>

        <Field label="Редкость">
          <Select
            value={rarity}
            onChange={(event) => setRarity(event.target.value as ItemRarity)}
          >
            {ITEM_RARITIES.map((value) => (
              <option key={value} value={value}>
                {RARITY_LABEL[value]}
              </option>
            ))}
          </Select>
        </Field>

        <Field label="Описание" full>
          <Textarea
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            rows={2}
            maxLength={1024}
            placeholder="Короткое описание для клиента"
          />
        </Field>
      </Section>

      <Section title="Привязка и совместимость">
        <Field
          label="Модель (model code)"
          hint="обязательно для машины"
          invalid={needsModelCode}
        >
          <Input
            value={modelCode}
            onChange={(event) => setModelCode(event.target.value)}
            placeholder="bmw_m5"
          />
        </Field>

        <Field label="Слот" hint="head / engine / primary …">
          <Input
            value={slot}
            onChange={(event) => setSlot(event.target.value)}
            placeholder="engine"
          />
        </Field>

        <Field label="Теги" hint="через запятую: sedan, sport, brand:bmw">
          <Input
            value={tagsText}
            onChange={(event) => setTagsText(event.target.value)}
            placeholder="sedan, class:sport"
          />
        </Field>

        <Field
          label="FitTags детали"
          hint="обязательно для деталей: model:bmw_m5, class:sedan, universal"
          invalid={needsFitTags}
        >
          <Input
            value={fitTagsText}
            onChange={(event) => setFitTagsText(event.target.value)}
            placeholder="class:sedan, class:sport"
          />
        </Field>
      </Section>

      <Section title="Характеристики">
        <div className="col-span-full space-y-2">
          {stats.map((stat, index) => (
            <div key={index} className="flex flex-wrap items-center gap-2">
              <Input
                value={stat.key}
                onChange={(event) => updateRow(setStats, index, { key: event.target.value })}
                placeholder="damage"
                className="max-w-xs"
              />
              <Input
                type="number"
                step="0.0001"
                value={stat.value}
                onChange={(event) => updateRow(setStats, index, { value: event.target.value })}
                placeholder="120"
                className="max-w-32"
              />
              <RemoveRow onClick={() => removeRow(setStats, index)} />
            </div>
          ))}
          <AddRow
            label="Добавить характеристику"
            onClick={() =>
              setStats((rows) => [...rows, { key: "", value: "" }])
            }
          />
        </div>
      </Section>

      <Section title="Цены">
        <div className="col-span-full space-y-2">
          {prices.map((price, index) => (
            <div key={index} className="flex flex-wrap items-center gap-2">
              <Input
                value={price.currencyCode}
                onChange={(event) =>
                  updateRow(setPrices, index, { currencyCode: event.target.value })
                }
                placeholder="SOFT"
                list="catalog-currencies"
                className="max-w-40"
              />
              <Input
                type="number"
                step={1}
                min={0}
                value={price.amount}
                onChange={(event) =>
                  updateRow(setPrices, index, { amount: event.target.value })
                }
                placeholder="1000"
                className="max-w-32"
              />
              <label className="inline-flex items-center gap-2 text-xs text-slate-600">
                <input
                  type="checkbox"
                  checked={price.isActive}
                  onChange={(event) =>
                    updateRow(setPrices, index, {
                      isActive: event.target.checked,
                    })
                  }
                  className="h-4 w-4 rounded border-slate-300"
                />
                активна
              </label>
              <RemoveRow onClick={() => removeRow(setPrices, index)} />
            </div>
          ))}
          <AddRow
            label="Добавить цену"
            onClick={() =>
              setPrices((rows) => [
                ...rows,
                { currencyCode: "", amount: "", isActive: true },
              ])
            }
          />
          <datalist id="catalog-currencies">
            {(currencies ?? []).map((currency) => (
              <option key={currency.code} value={currency.code}>
                {currency.displayName}
              </option>
            ))}
          </datalist>
        </div>
      </Section>

      <Section title="Окна доступности">
        <div className="col-span-full space-y-2">
          {availability.map((window, index) => (
            <div key={index} className="flex flex-wrap items-center gap-2">
              <Input
                type="datetime-local"
                value={window.startsAt}
                onChange={(event) =>
                  updateRow(setAvailability, index, {
                    startsAt: event.target.value,
                  })
                }
                className="max-w-56"
              />
              <span className="text-xs text-slate-400">→</span>
              <Input
                type="datetime-local"
                value={window.endsAt}
                onChange={(event) =>
                  updateRow(setAvailability, index, {
                    endsAt: event.target.value,
                  })
                }
                className="max-w-56"
              />
              <Input
                value={window.channel}
                onChange={(event) =>
                  updateRow(setAvailability, index, {
                    channel: event.target.value,
                  })
                }
                placeholder="event-summer"
                className="max-w-44"
              />
              <RemoveRow onClick={() => removeRow(setAvailability, index)} />
            </div>
          ))}
          <AddRow
            label="Добавить окно"
            onClick={() =>
              setAvailability((rows) => [
                ...rows,
                { startsAt: "", endsAt: "", channel: "" },
              ])
            }
          />
        </div>
      </Section>

      {error ? (
        <ApiErrorList error={error} fallback="Не удалось сохранить предмет" />
      ) : null}

      <footer className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 pt-4">
        <div className="flex gap-2">
          <Button type="submit" disabled={isPending || !isValid}>
            <Save className="mr-1.5 h-4 w-4" aria-hidden="true" />
            {isPending ? "Сохраняем..." : "Сохранить"}
          </Button>
          <Button type="button" variant="ghost" onClick={onCancel}>
            Отмена
          </Button>
        </div>

        {item && onArchive && item.status !== "ARCHIVED" && (
          <button
            type="button"
            onClick={() => setArchiveOpen((open) => !open)}
            className="inline-flex items-center gap-1.5 text-xs font-medium text-rose-600 underline-offset-2 hover:underline"
          >
            <Trash2 className="h-3.5 w-3.5" aria-hidden="true" />
            Архивировать
          </button>
        )}

        {item && onReactivate && item.status === "ARCHIVED" && (
          <button
            type="button"
            onClick={() => void onReactivateClick()}
            disabled={isPending}
            className="inline-flex items-center gap-1.5 text-xs font-medium text-emerald-700 underline-offset-2 hover:underline disabled:opacity-50"
          >
            <RotateCcw className="h-3.5 w-3.5" aria-hidden="true" />
            Вернуть в работу
          </button>
        )}
      </footer>

      {archiveOpen && item && (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-rose-200 bg-rose-50 p-3">
          <p className="text-xs text-rose-800">
            Предмет будет снят с публикации и скрыт из клиентского каталога.
          </p>
          <Button
            type="button"
            variant="ghost"
            className="text-rose-700 hover:bg-rose-100"
            onClick={() => void onArchiveClick()}
            disabled={isPending}
          >
            {isPending ? "Архивируем..." : "Да, архивировать"}
          </Button>
        </div>
      )}
    </form>
  );
}

function Section({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <fieldset className="grid gap-3 sm:grid-cols-2">
      <legend className="col-span-full text-[11px] font-semibold tracking-wide text-slate-400 uppercase">
        {title}
      </legend>
      {children}
    </fieldset>
  );
}

function Field({
  label,
  hint,
  invalid,
  full,
  children,
}: {
  label: string;
  hint?: string;
  invalid?: boolean;
  full?: boolean;
  children: React.ReactNode;
}) {
  return (
    <label className={full ? "block sm:col-span-2" : "block"}>
      <span className="mb-1 block text-xs font-medium text-slate-600">
        {label}
        {hint && <span className="ml-1 font-normal text-slate-400">· {hint}</span>}
      </span>
      {children}
      {invalid && (
        <span className="mt-1 block text-[11px] text-rose-600">
          Заполните это поле — обязательное для выбранного типа.
        </span>
      )}
    </label>
  );
}

function AddRow({ label, onClick }: { label: string; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="inline-flex items-center gap-1.5 rounded-md border border-dashed border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-600 transition-colors hover:border-slate-400 hover:bg-slate-50"
    >
      <Plus className="h-3.5 w-3.5" aria-hidden="true" />
      {label}
    </button>
  );
}

function RemoveRow({ onClick }: { onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label="Удалить строку"
      className="inline-flex h-8 w-8 items-center justify-center rounded-md text-slate-400 transition-colors hover:bg-rose-50 hover:text-rose-600"
    >
      <Minus className="h-4 w-4" aria-hidden="true" />
    </button>
  );
}

function updateRow<T>(
  setRows: React.Dispatch<React.SetStateAction<T[]>>,
  index: number,
  patch: Partial<T>,
) {
  setRows((rows) =>
    rows.map((row, i) => (i === index ? { ...row, ...patch } : row)),
  );
}

function removeRow<T>(
  setRows: React.Dispatch<React.SetStateAction<T[]>>,
  index: number,
) {
  setRows((rows) => rows.filter((_, i) => i !== index));
}

function splitList(value: string): string[] {
  const seen = new Set<string>();
  const result: string[] = [];

  for (const raw of value.split(",")) {
    const tag = raw.trim().toLowerCase();
    if (tag && !seen.has(tag)) {
      seen.add(tag);
      result.push(tag);
    }
  }

  return result;
}

function toStats(rows: StatDraft[]): ItemStat[] | null {
  const result = rows
    .filter((row) => row.key.trim().length > 0)
    .map((row) => ({ key: row.key.trim(), value: Number(row.value) || 0 }));

  return result.length > 0 ? result : null;
}

function toPrices(rows: PriceDraft[]): ItemPrice[] | null {
  const result = rows
    .filter((row) => row.currencyCode.trim().length > 0)
    .map((row) => ({
      currencyCode: row.currencyCode.trim().toUpperCase(),
      amount: Math.max(0, Math.trunc(Number(row.amount) || 0)),
      isActive: row.isActive,
      validFrom: null,
    }));

  return result.length > 0 ? result : null;
}

function toAvailability(rows: AvailabilityDraft[]): AvailabilityWindow[] | null {
  const result = rows
    .filter((row) => row.startsAt || row.endsAt || row.channel.trim())
    .map((row) => ({
      startsAt: row.startsAt ? new Date(row.startsAt).toISOString() : null,
      endsAt: row.endsAt ? new Date(row.endsAt).toISOString() : null,
      channel: row.channel.trim() || null,
    }));

  return result.length > 0 ? result : null;
}

function toLocalInput(value: string | null): string {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";

  const pad = (n: number) => String(n).padStart(2, "0");

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
