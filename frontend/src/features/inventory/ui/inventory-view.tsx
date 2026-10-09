"use client";

import type { Car, OwnedItem } from "@/src/entities/inventory";
import { ErrorType, isEnvelopeError } from "@/src/shared/api/errors";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Spinner } from "@/src/shared/ui/spinner";
import { Car as CarIcon, Package, Shirt, Wrench } from "lucide-react";
import { useMyInventory } from "../model/use-my-inventory";

const KIND_LABEL: Record<string, string> = {
  CLOTHING: "Одежда",
  CAR: "Машина",
  CAR_PART: "Деталь",
};

export function InventoryView() {
  const { inventory, isPending, error } = useMyInventory();

  if (isPending) {
    return <Spinner label="Загружаем инвентарь..." />;
  }

  if (isEnvelopeError(error) && error.type === ErrorType.NOT_FOUND) {
    return (
      <p className="rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
        Инвентарь ещё не создан. Он появится после провижининга аккаунта.
      </p>
    );
  }

  if (error) {
    return (
      <ApiErrorList error={error} fallback="Не удалось загрузить инвентарь" />
    );
  }

  if (!inventory) return null;

  const codeById = new Map(inventory.items.map((item) => [item.itemId, item.code]));

  return (
    <div className="space-y-6">
      <Section title="Владение" icon={<Package className="h-4 w-4" />}>
        {inventory.items.length === 0 ? (
          <Empty>Предметов пока нет.</Empty>
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {inventory.items.map((item) => (
              <ItemCard key={item.itemId} item={item} />
            ))}
          </ul>
        )}
      </Section>

      <Section title="Аутфит" icon={<Shirt className="h-4 w-4" />}>
        {inventory.outfit.length === 0 ? (
          <Empty>Ничего не надето.</Empty>
        ) : (
          <ul className="grid gap-2 sm:grid-cols-2">
            {inventory.outfit.map((entry) => (
              <li
                key={entry.slot}
                className="flex items-center justify-between rounded-md border border-slate-200 bg-white px-3 py-2 text-sm"
              >
                <span className="font-medium text-slate-700">{entry.slot}</span>
                <span className="font-mono text-xs text-slate-500">
                  {codeById.get(entry.itemId) ?? entry.itemId.slice(0, 8)}
                </span>
              </li>
            ))}
          </ul>
        )}
      </Section>

      <Section title="Гараж" icon={<CarIcon className="h-4 w-4" />}>
        {inventory.cars.length === 0 ? (
          <Empty>Машин пока нет.</Empty>
        ) : (
          <ul className="space-y-3">
            {inventory.cars.map((car) => (
              <CarCard key={car.id} car={car} codeById={codeById} />
            ))}
          </ul>
        )}
      </Section>
    </div>
  );
}

function Section({
  title,
  icon,
  children,
}: {
  title: string;
  icon: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-5">
      <h2 className="mb-3 flex items-center gap-2 text-xs font-semibold tracking-wide text-slate-500 uppercase">
        {icon}
        {title}
      </h2>
      {children}
    </section>
  );
}

function ItemCard({ item }: { item: OwnedItem }) {
  return (
    <li className="rounded-lg border border-slate-200 bg-white p-3">
      <div className="flex items-start justify-between gap-2">
        <p className="truncate text-sm font-medium text-slate-900">
          {item.code}
        </p>
        <span className="shrink-0 rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-medium text-slate-600">
          {KIND_LABEL[item.kind] ?? item.kind}
        </span>
      </div>
      <p className="mt-1 text-xs text-slate-500">
        {item.slot ? `слот: ${item.slot}` : "—"} · ×{item.quantity}
      </p>
      <p className="text-[11px] text-slate-400">источник: {item.source}</p>
    </li>
  );
}

function CarCard({ car, codeById }: { car: Car; codeById: Map<string, string> }) {
  return (
    <li className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm font-medium text-slate-900">
          {codeById.get(car.carItemId) ?? car.code}
        </p>
        <span className="text-xs text-slate-500">
          {[car.color, car.plate].filter(Boolean).join(" · ") || "—"}
        </span>
      </div>
      {car.installations.length === 0 ? (
        <p className="mt-2 text-xs text-slate-400">тюнинг не установлен</p>
      ) : (
        <ul className="mt-2 flex flex-wrap gap-2">
          {car.installations.map((installation) => (
            <li
              key={installation.slot}
              className="inline-flex items-center gap-1.5 rounded-md bg-slate-50 px-2 py-1 text-xs text-slate-600"
            >
              <Wrench className="h-3 w-3" aria-hidden="true" />
              {installation.slot}:{" "}
              <span className="font-mono">
                {codeById.get(installation.partItemId) ??
                  installation.partItemId.slice(0, 8)}
              </span>
            </li>
          ))}
        </ul>
      )}
    </li>
  );
}

function Empty({ children }: { children: React.ReactNode }) {
  return (
    <p className="rounded-md border border-dashed border-slate-300 px-4 py-6 text-center text-sm text-slate-500">
      {children}
    </p>
  );
}
