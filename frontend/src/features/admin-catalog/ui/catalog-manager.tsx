"use client";

import {
  ITEM_KINDS,
  ITEM_STATUSES,
  type ItemDetail,
  type ItemKind,
  type ItemStatus,
} from "@/src/entities/catalog";
import { cn } from "@/src/shared/lib/utils";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Input } from "@/src/shared/ui/input";
import { Select } from "@/src/shared/ui/select";
import { Spinner } from "@/src/shared/ui/spinner";
import { History, Package, Plus, Search, Upload } from "lucide-react";
import { useMemo, useState } from "react";
import { useAdminCatalogItems } from "../model/use-admin-catalog-items";
import { useArchiveItem } from "../model/use-archive-item";
import { useCatalogVersions } from "../model/use-catalog-versions";
import { useCurrentVersion } from "../model/use-current-version";
import { usePublishVersion } from "../model/use-publish-version";
import { useReactivateItem } from "../model/use-reactivate-item";
import { useRollbackVersion } from "../model/use-rollback-version";
import { useSaveItem, type SaveItemInput } from "../model/use-save-item";
import { KIND_META, KindBadge, RarityBadge, StatusBadge, STATUS_META } from "./catalog-badges";
import { ItemForm } from "./item-form";
import { PublishVersionDialog } from "./publish-version-dialog";
import { VersionsDialog } from "./versions-dialog";

export function CatalogManager() {
  const [search, setSearch] = useState("");
  const [kind, setKind] = useState<ItemKind | "">("");
  const [status, setStatus] = useState<ItemStatus | "">("");
  const [selected, setSelected] = useState<ItemDetail | null>(null);
  const [creating, setCreating] = useState(false);
  const [publishOpen, setPublishOpen] = useState(false);
  const [versionsOpen, setVersionsOpen] = useState(false);

  const { items, isPending, isFetching, error } = useAdminCatalogItems({
    kind: kind || undefined,
    status: status || undefined,
  });
  const { version } = useCurrentVersion();
  const {
    versions,
    isPending: versionsLoading,
    error: versionsError,
  } = useCatalogVersions();

  const save = useSaveItem();
  const archive = useArchiveItem();
  const reactivate = useReactivateItem();
  const publish = usePublishVersion();
  const rollback = useRollbackVersion();

  const filtered = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) return items;

    return items.filter(
      (item) =>
        item.displayName.toLowerCase().includes(query) ||
        item.code.toLowerCase().includes(query),
    );
  }, [items, search]);

  const onSelect = (item: ItemDetail) => {
    save.reset();
    archive.reset();
    reactivate.reset();
    setCreating(false);
    setSelected(item);
  };

  const onCreate = () => {
    save.reset();
    archive.reset();
    reactivate.reset();
    setSelected(null);
    setCreating(true);
  };

  const onCancel = () => {
    setSelected(null);
    setCreating(false);
  };

  const onSave = async (input: SaveItemInput) => {
    const saved = await save.saveItem(input);
    setSelected(saved);
    setCreating(false);
    return saved;
  };

  const onArchive = async (itemId: string) => {
    const result = await archive.archiveItem(itemId);
    setSelected(null);
    setCreating(false);
    return result;
  };

  const onReactivate = async (itemId: string) => {
    const updated = await reactivate.reactivateItem(itemId);
    setSelected(updated);
    return updated;
  };

  const formKey = creating ? "create" : (selected?.id ?? "empty");

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl bg-gradient-to-r from-slate-900 via-slate-800 to-slate-700 p-5 text-white shadow-sm">
        <div className="space-y-1">
          <p className="text-[11px] font-medium tracking-widest text-slate-300 uppercase">
            Текущая версия каталога
          </p>
          <p className="text-2xl font-semibold tracking-tight">
            {version ? `v${version.number}` : "черновик"}
          </p>
          <p className="text-xs text-slate-300">
            {version
              ? `${version.itemCount} предметов · ${formatDate(version.publishedAt)}`
              : "Каталог ещё не публиковался — клиенты не видят предметы."}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button
            onClick={() => {
              rollback.reset();
              setVersionsOpen(true);
            }}
            className="border border-slate-500 bg-transparent text-white hover:bg-slate-800"
          >
            <History className="mr-1.5 h-4 w-4" aria-hidden="true" />
            Версии
          </Button>
          <Button
            onClick={() => {
              publish.reset();
              setPublishOpen(true);
            }}
            className="bg-white text-slate-900 hover:bg-slate-100"
          >
            <Upload className="mr-1.5 h-4 w-4" aria-hidden="true" />
            Опубликовать версию
          </Button>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3 rounded-xl border border-slate-200 bg-white p-3">
        <div className="relative min-w-48 flex-1">
          <Search
            className="pointer-events-none absolute top-1/2 left-2.5 h-4 w-4 -translate-y-1/2 text-slate-400"
            aria-hidden="true"
          />
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Поиск по названию или коду"
            className="pl-8"
            aria-label="Поиск предметов"
          />
        </div>

        <Select
          value={kind}
          onChange={(event) => setKind(event.target.value as ItemKind | "")}
          className="w-44"
          aria-label="Фильтр по типу"
        >
          <option value="">Все типы</option>
          {ITEM_KINDS.map((value) => (
            <option key={value} value={value}>
              {KIND_META[value].label}
            </option>
          ))}
        </Select>

        <Select
          value={status}
          onChange={(event) => setStatus(event.target.value as ItemStatus | "")}
          className="w-40"
          aria-label="Фильтр по статусу"
        >
          <option value="">Все статусы</option>
          {ITEM_STATUSES.map((value) => (
            <option key={value} value={value}>
              {STATUS_META[value].label}
            </option>
          ))}
        </Select>

        <Button onClick={onCreate}>
          <Plus className="mr-1.5 h-4 w-4" aria-hidden="true" />
          Создать
        </Button>
      </div>

      <div className="grid gap-5 lg:grid-cols-12">
        <aside className="space-y-2 lg:col-span-4">
          <div className="flex items-center justify-between px-1">
            <p className="text-xs font-medium text-slate-500">
              Предметов: {filtered.length}
            </p>
            {isFetching && <Spinner />}
          </div>

          {error ? (
            <ApiErrorList error={error} fallback="Не удалось загрузить каталог" />
          ) : isPending ? (
            <Spinner label="Загружаем каталог..." />
          ) : filtered.length === 0 ? (
            <p className="rounded-lg border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500">
              Ничего не найдено.
            </p>
          ) : (
            <ul className="space-y-2">
              {filtered.map((item) => (
                <li key={item.id}>
                  <button
                    type="button"
                    onClick={() => onSelect(item)}
                    className={cn(
                      "w-full rounded-xl border bg-white p-3 text-left transition-all",
                      selected?.id === item.id
                        ? "border-slate-900 ring-1 ring-slate-900"
                        : "border-slate-200 hover:border-slate-300 hover:shadow-sm",
                    )}
                  >
                    <div className="flex items-center justify-between gap-2">
                      <KindBadge kind={item.kind} />
                      <RarityBadge rarity={item.rarity} />
                    </div>
                    <p className="mt-2 truncate text-sm font-medium text-slate-900">
                      {item.displayName}
                    </p>
                    <p className="truncate font-mono text-[11px] text-slate-400">
                      {item.code}
                    </p>
                    <div className="mt-2 flex items-center justify-between">
                      <StatusBadge status={item.status} />
                      <span className="text-xs text-slate-500">
                        {priceLabel(item)}
                      </span>
                    </div>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </aside>

        <section className="lg:col-span-8">
          {creating || selected ? (
            <ItemForm
              key={formKey}
              item={creating ? null : selected}
              isPending={
                save.isPending || archive.isPending || reactivate.isPending
              }
              error={save.error ?? archive.error ?? reactivate.error}
              onSave={onSave}
              onArchive={onArchive}
              onReactivate={onReactivate}
              onCancel={onCancel}
              reset={() => {
                save.reset();
                archive.reset();
                reactivate.reset();
              }}
            />
          ) : (
            <div className="flex h-full min-h-72 flex-col items-center justify-center gap-3 rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center">
              <span className="inline-flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                <Package className="h-6 w-6" aria-hidden="true" />
              </span>
              <div>
                <p className="text-sm font-medium text-slate-700">
                  Выберите предмет или создайте новый
                </p>
                <p className="mt-1 text-xs text-slate-500">
                  Слева — список каталога, справа — редактор выбранного предмета.
                </p>
              </div>
              <Button onClick={onCreate}>
                <Plus className="mr-1.5 h-4 w-4" aria-hidden="true" />
                Создать предмет
              </Button>
            </div>
          )}
        </section>
      </div>

      <PublishVersionDialog
        open={publishOpen}
        isPending={publish.isPending}
        error={publish.error}
        onPublish={publish.publishVersion}
        onClose={() => setPublishOpen(false)}
      />

      <VersionsDialog
        open={versionsOpen}
        versions={versions}
        isLoading={versionsLoading}
        loadError={versionsError}
        isPending={rollback.isPending}
        error={rollback.error}
        onRollback={(targetNumber, note) =>
          rollback.rollbackVersion({ targetNumber, note })
        }
        onClose={() => setVersionsOpen(false)}
      />
    </div>
  );
}

function priceLabel(item: ItemDetail): string {
  const active = item.prices.filter((price) => price.isActive);
  if (active.length === 0) return "без цены";

  const cheapest = active.reduce((min, price) =>
    price.amount < min.amount ? price : min,
  );

  return `${cheapest.amount.toLocaleString("ru-RU")} ${cheapest.currencyCode}`;
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
