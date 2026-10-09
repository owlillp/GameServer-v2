import { Car, Shirt, Wrench, type LucideIcon } from "lucide-react";
import type { ItemKind, ItemRarity, ItemStatus } from "@/src/entities/catalog";

export const KIND_META: Record<
  ItemKind,
  { label: string; icon: LucideIcon; accent: string }
> = {
  CLOTHING: { label: "Одежда", icon: Shirt, accent: "text-rose-600" },
  CAR: { label: "Машина", icon: Car, accent: "text-sky-600" },
  CAR_PART: { label: "Деталь", icon: Wrench, accent: "text-amber-600" },
};

export const RARITY_CLASS: Record<ItemRarity, string> = {
  COMMON: "bg-slate-100 text-slate-600 ring-slate-200",
  UNCOMMON: "bg-emerald-50 text-emerald-700 ring-emerald-200",
  RARE: "bg-sky-50 text-sky-700 ring-sky-200",
  EPIC: "bg-violet-50 text-violet-700 ring-violet-200",
  LEGENDARY: "bg-amber-50 text-amber-700 ring-amber-200",
};

export const RARITY_LABEL: Record<ItemRarity, string> = {
  COMMON: "Обычный",
  UNCOMMON: "Необычный",
  RARE: "Редкий",
  EPIC: "Эпический",
  LEGENDARY: "Легендарный",
};

export const STATUS_META: Record<
  ItemStatus,
  { label: string; className: string }
> = {
  DRAFT: {
    label: "Черновик",
    className: "bg-amber-50 text-amber-700 ring-amber-200",
  },
  PUBLISHED: {
    label: "Опубликован",
    className: "bg-emerald-50 text-emerald-700 ring-emerald-200",
  },
  ARCHIVED: {
    label: "Архив",
    className: "bg-slate-100 text-slate-500 ring-slate-200",
  },
};

export function KindBadge({ kind }: { kind: ItemKind }) {
  const meta = KIND_META[kind] ?? KIND_META.CLOTHING;
  const Icon = meta.icon;

  return (
    <span className={`inline-flex items-center gap-1.5 ${meta.accent}`}>
      <Icon className="h-4 w-4" aria-hidden="true" />
      <span className="text-xs font-medium text-slate-600">{meta.label}</span>
    </span>
  );
}

export function RarityBadge({ rarity }: { rarity: ItemRarity }) {
  const rarityClass = RARITY_CLASS[rarity] ?? RARITY_CLASS.COMMON;

  return (
    <span
      className={`inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium ring-1 ring-inset ${rarityClass}`}
    >
      {RARITY_LABEL[rarity] ?? rarity}
    </span>
  );
}

export function StatusBadge({ status }: { status: ItemStatus }) {
  const meta = STATUS_META[status] ?? STATUS_META.DRAFT;

  return (
    <span
      className={`inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium ring-1 ring-inset ${meta.className}`}
    >
      {meta.label}
    </span>
  );
}
