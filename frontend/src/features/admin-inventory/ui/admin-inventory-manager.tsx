"use client";

import { adminCatalogItemsQueryOptions } from "@/src/entities/catalog";
import type {
  Car,
  Inventory,
  OwnedItem,
} from "@/src/entities/inventory";
import { ErrorType, isEnvelopeError } from "@/src/shared/api/errors";
import { cn } from "@/src/shared/lib/utils";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Input } from "@/src/shared/ui/input";
import { Select } from "@/src/shared/ui/select";
import { Spinner } from "@/src/shared/ui/spinner";
import { useQuery } from "@tanstack/react-query";
import { Car as CarIcon, Plus, RefreshCw, Shirt, Trash2, Wrench } from "lucide-react";
import { useState, type FormEvent } from "react";
import { useAdminInventory } from "../model/use-admin-inventory";
import { useInventoryMutations } from "../model/use-inventory-mutations";

const OUTFIT_SLOTS = ["head", "torso", "legs", "shoes", "accessory"] as const;
const PART_SLOTS = [
  "engine",
  "turbo",
  "suspension",
  "brakes",
  "tires",
  "nitro",
  "body",
  "bumper",
] as const;

export function AdminInventoryManager() {
  const [accountInput, setAccountInput] = useState("");
  const [accountId, setAccountId] = useState<string | null>(null);

  const { inventory, isPending, isFetching, error } =
    useAdminInventory(accountId);
  const actions = useInventoryMutations(accountId);

  const notFound = isEnvelopeError(error) && error.type === ErrorType.NOT_FOUND;

  return (
    <div className="space-y-5">
      <form
        onSubmit={(event: FormEvent) => {
          event.preventDefault();
          actions.reset();
          setAccountId(accountInput.trim() || null);
        }}
        className="flex flex-wrap items-end gap-3 rounded-xl border border-slate-200 bg-white p-4"
      >
        <label className="min-w-72 flex-1">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            ID аккаунта игрока (accountId)
          </span>
          <Input
            value={accountInput}
            onChange={(event) => setAccountInput(event.target.value)}
            placeholder="00000000-0000-0000-0000-000000000000"
          />
        </label>
        <Button type="submit">
          <RefreshCw className="mr-1.5 h-4 w-4" aria-hidden="true" />
          Загрузить
        </Button>
      </form>

      {!accountId ? (
        <p className="rounded-lg border border-dashed border-slate-300 px-4 py-10 text-center text-sm text-slate-500">
          Введи ID аккаунта, чтобы посмотреть и отредактировать инвентарь.
        </p>
      ) : isPending ? (
        <Spinner label="Загружаем инвентарь..." />
      ) : error ? (
        <div className="space-y-2">
          <ApiErrorList
            error={error}
            fallback={
              notFound
                ? "Инвентарь не найден — у игрока ещё нет предметов."
                : "Не удалось загрузить инвентарь"
            }
          />
          <GrantPanel accountId={accountId} actions={actions} />
        </div>
      ) : !inventory ? (
        <GrantPanel accountId={accountId} actions={actions} />
      ) : (
        <div className="space-y-5" aria-busy={isFetching}>
          <GrantPanel accountId={accountId} actions={actions} />

          <ItemsPanel inventory={inventory} actions={actions} />

          <OutfitPanel inventory={inventory} actions={actions} />

          <CarsPanel inventory={inventory} actions={actions} />
        </div>
      )}
    </div>
  );
}

type Actions = ReturnType<typeof useInventoryMutations>;

function Panel({
  title,
  icon,
  children,
}: {
  title: string;
  icon: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <section className="rounded-xl border border-slate-200 bg-white p-5">
      <h2 className="mb-4 flex items-center gap-2 text-xs font-semibold tracking-wide text-slate-500 uppercase">
        {icon}
        {title}
      </h2>
      {children}
    </section>
  );
}

function GrantPanel({
  accountId,
  actions,
}: {
  accountId: string;
  actions: Actions;
}) {
  const { data: catalogItems } = useQuery(adminCatalogItemsQueryOptions({}));
  const published = (catalogItems ?? []).filter(
    (item) => item.status !== "ARCHIVED",
  );

  const [itemId, setItemId] = useState("");
  const [quantity, setQuantity] = useState("1");

  const selected = published.find((item) => item.id === itemId);

  return (
    <Panel title="Выдать предметы" icon={<Plus className="h-4 w-4" />}>
      <form
        onSubmit={async (event) => {
          event.preventDefault();
          if (!selected) return;
          actions.reset();
          try {
            await actions.run({
              type: "grant",
              request: {
                source: "Admin",
                operationId: crypto.randomUUID(),
                referenceId: null,
                items: [
                  {
                    itemId: selected.id,
                    code: selected.code,
                    kind: selected.kind,
                    slot: selected.slot,
                    quantity: Math.max(1, Number(quantity) || 1),
                  },
                ],
              },
            });
            setItemId("");
            setQuantity("1");
          } catch {
            // ошибка через ApiErrorList
          }
        }}
        className="grid gap-3 sm:grid-cols-[1fr_8rem_auto] sm:items-end"
      >
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Предмет каталога
          </span>
          <Select
            value={itemId}
            onChange={(event) => setItemId(event.target.value)}
            required
          >
            <option value="" disabled>
              Выберите предмет
            </option>
            {published.map((item) => (
              <option key={item.id} value={item.id}>
                {item.displayName} · {item.code} ({item.kind})
              </option>
            ))}
          </Select>
        </label>
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Кол-во
          </span>
          <Input
            type="number"
            min={1}
            step={1}
            value={quantity}
            onChange={(event) => setQuantity(event.target.value)}
          />
        </label>
        <Button type="submit" disabled={actions.isPending || !selected}>
          {actions.isPending ? "..." : "Выдать"}
        </Button>
      </form>
      <p className="mt-2 text-[11px] text-slate-400">accountId: {accountId}</p>
      {actions.error ? (
        <div className="mt-3">
          <ApiErrorList error={actions.error} fallback="Операция не удалась" />
        </div>
      ) : null}
    </Panel>
  );
}

function ItemsPanel({
  inventory,
  actions,
}: {
  inventory: Inventory;
  actions: Actions;
}) {
  return (
    <Panel title={`Владение (${inventory.items.length})`} icon={<Shirt className="h-4 w-4" />}>
      {inventory.items.length === 0 ? (
        <p className="text-sm text-slate-500">Предметов нет.</p>
      ) : (
        <ul className="divide-y divide-slate-100 rounded-md border border-slate-200">
          {inventory.items.map((item) => (
            <ItemRow key={item.itemId} item={item} actions={actions} />
          ))}
        </ul>
      )}
    </Panel>
  );
}

function ItemRow({ item, actions }: { item: OwnedItem; actions: Actions }) {
  return (
    <li className="flex flex-wrap items-center justify-between gap-2 px-3 py-2.5 text-sm">
      <span className="flex items-center gap-2">
        <span className="font-medium text-slate-800">{item.code}</span>
        <span className="rounded bg-slate-100 px-1.5 py-0.5 text-[11px] text-slate-600">
          {item.kind}
        </span>
        {item.slot ? (
          <span className="text-xs text-slate-400">слот {item.slot}</span>
        ) : null}
        <span className="text-xs text-slate-400">×{item.quantity}</span>
      </span>
      <button
        type="button"
        disabled={actions.isPending}
        onClick={() =>
          void actions
            .run({ type: "revoke", request: { itemIds: [item.itemId] } })
            .catch(() => undefined)
        }
        className="inline-flex items-center gap-1 text-xs font-medium text-rose-600 underline-offset-2 hover:underline disabled:opacity-50"
      >
        <Trash2 className="h-3.5 w-3.5" aria-hidden="true" />
        Отозвать
      </button>
    </li>
  );
}

function OutfitPanel({
  inventory,
  actions,
}: {
  inventory: Inventory;
  actions: Actions;
}) {
  const clothing = inventory.items.filter((item) => item.kind === "CLOTHING");
  const [slot, setSlot] = useState<string>(OUTFIT_SLOTS[0]);
  const [itemId, setItemId] = useState("");

  const codeById = new Map(inventory.items.map((item) => [item.itemId, item.code]));

  return (
    <Panel title="Аутфит" icon={<Shirt className="h-4 w-4" />}>
      {inventory.outfit.length === 0 ? (
        <p className="text-sm text-slate-500">Ничего не надето.</p>
      ) : (
        <ul className="mb-4 grid gap-2 sm:grid-cols-2">
          {inventory.outfit.map((entry) => (
            <li
              key={entry.slot}
              className="flex items-center justify-between rounded-md border border-slate-200 px-3 py-2 text-sm"
            >
              <span>
                <span className="font-medium text-slate-700">{entry.slot}</span>{" "}
                <span className="font-mono text-xs text-slate-500">
                  {codeById.get(entry.itemId) ?? entry.itemId.slice(0, 8)}
                </span>
              </span>
              <button
                type="button"
                disabled={actions.isPending}
                onClick={() =>
                  void actions
                    .run({ type: "unequip", slot: entry.slot })
                    .catch(() => undefined)
                }
                className="text-xs font-medium text-rose-600 hover:underline disabled:opacity-50"
              >
                Снять
              </button>
            </li>
          ))}
        </ul>
      )}

      <form
        onSubmit={async (event) => {
          event.preventDefault();
          actions.reset();
          try {
            await actions.run({ type: "equip", slot, itemId });
            setItemId("");
          } catch {
            // через ApiErrorList
          }
        }}
        className="grid gap-3 sm:grid-cols-[10rem_1fr_auto] sm:items-end"
      >
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Слот
          </span>
          <Select value={slot} onChange={(event) => setSlot(event.target.value)}>
            {OUTFIT_SLOTS.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </Select>
        </label>
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Предмет одежды
          </span>
          <Select
            value={itemId}
            onChange={(event) => setItemId(event.target.value)}
            required
          >
            <option value="" disabled>
              {clothing.length === 0
                ? "Нет предметов одежды"
                : "Выберите предмет"}
            </option>
            {clothing.map((item) => (
              <option key={item.itemId} value={item.itemId}>
                {item.code}
              </option>
            ))}
          </Select>
        </label>
        <Button type="submit" disabled={actions.isPending || !itemId}>
          Надеть
        </Button>
      </form>
    </Panel>
  );
}

function CarsPanel({
  inventory,
  actions,
}: {
  inventory: Inventory;
  actions: Actions;
}) {
  const carItems = inventory.items.filter((item) => item.kind === "CAR");
  const [carItemId, setCarItemId] = useState("");
  const [color, setColor] = useState("");
  const [plate, setPlate] = useState("");

  const selectedCar = carItems.find((item) => item.itemId === carItemId);

  return (
    <Panel title={`Гараж (${inventory.cars.length})`} icon={<CarIcon className="h-4 w-4" />}>
      {inventory.cars.length === 0 ? (
        <p className="text-sm text-slate-500">Машин нет.</p>
      ) : (
        <ul className="mb-4 space-y-3">
          {inventory.cars.map((car) => (
            <CarRow
              key={car.id}
              car={car}
              inventory={inventory}
              actions={actions}
            />
          ))}
        </ul>
      )}

      <form
        onSubmit={async (event) => {
          event.preventDefault();
          if (!selectedCar) return;
          actions.reset();
          try {
            await actions.run({
              type: "addCar",
              request: {
                carItemId: selectedCar.itemId,
                code: selectedCar.code,
                color: color || null,
                plate: plate || null,
              },
            });
            setCarItemId("");
            setColor("");
            setPlate("");
          } catch {
            // через ApiErrorList
          }
        }}
        className="grid gap-3 sm:grid-cols-[1fr_7rem_7rem_auto] sm:items-end"
      >
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Машина (из владения)
          </span>
          <Select
            value={carItemId}
            onChange={(event) => setCarItemId(event.target.value)}
            required
          >
            <option value="" disabled>
              {carItems.length === 0 ? "Нет машин во владении" : "Выберите машину"}
            </option>
            {carItems.map((item) => (
              <option key={item.itemId} value={item.itemId}>
                {item.code}
              </option>
            ))}
          </Select>
        </label>
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Цвет
          </span>
          <Input
            value={color}
            onChange={(event) => setColor(event.target.value)}
            placeholder="black"
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Номер
          </span>
          <Input
            value={plate}
            onChange={(event) => setPlate(event.target.value)}
            placeholder="A123BC"
            maxLength={16}
          />
        </label>
        <Button type="submit" disabled={actions.isPending || !selectedCar}>
          <Plus className="mr-1 h-4 w-4" aria-hidden="true" />
          Добавить
        </Button>
      </form>
    </Panel>
  );
}

function CarRow({
  car,
  inventory,
  actions,
}: {
  car: Car;
  inventory: Inventory;
  actions: Actions;
}) {
  const parts = inventory.items.filter((item) => item.kind === "CAR_PART");
  const codeById = new Map(inventory.items.map((item) => [item.itemId, item.code]));

  const [slot, setSlot] = useState<string>(PART_SLOTS[0]);
  const [partItemId, setPartItemId] = useState("");

  const carCode = codeById.get(car.carItemId) ?? car.code;

  return (
    <li className="rounded-lg border border-slate-200 p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="flex items-center gap-2 text-sm font-medium text-slate-900">
          <CarIcon className="h-4 w-4 text-sky-600" aria-hidden="true" />
          {carCode}
          <span className="text-xs font-normal text-slate-500">
            {[car.color, car.plate].filter(Boolean).join(" · ") || "—"}
          </span>
        </p>
        <button
          type="button"
          disabled={actions.isPending}
          onClick={() =>
            void actions
              .run({ type: "removeCar", carId: car.id })
              .catch(() => undefined)
          }
          className="inline-flex items-center gap-1 text-xs font-medium text-rose-600 hover:underline disabled:opacity-50"
        >
          <Trash2 className="h-3.5 w-3.5" aria-hidden="true" />
          Убрать машину
        </button>
      </div>

      {car.installations.length > 0 && (
        <ul className="mt-3 flex flex-wrap gap-2">
          {car.installations.map((installation) => (
            <li
              key={installation.slot}
              className={cn(
                "inline-flex items-center gap-1.5 rounded-md bg-slate-50 px-2 py-1 text-xs text-slate-600",
              )}
            >
              <Wrench className="h-3 w-3" aria-hidden="true" />
              {installation.slot}:{" "}
              {codeById.get(installation.partItemId) ??
                installation.partItemId.slice(0, 8)}
              <button
                type="button"
                disabled={actions.isPending}
                onClick={() =>
                  void actions
                    .run({
                      type: "removePart",
                      carId: car.id,
                      slot: installation.slot,
                    })
                    .catch(() => undefined)
                }
                className="ml-1 text-rose-500 hover:underline disabled:opacity-50"
                aria-label={`Снять ${installation.slot}`}
              >
                ✕
              </button>
            </li>
          ))}
        </ul>
      )}

      <form
        onSubmit={async (event) => {
          event.preventDefault();
          actions.reset();
          try {
            await actions.run({
              type: "installPart",
              carId: car.id,
              slot,
              partItemId,
            });
            setPartItemId("");
          } catch {
            // через ApiErrorList
          }
        }}
        className="mt-3 grid gap-2 sm:grid-cols-[9rem_1fr_auto] sm:items-end"
      >
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Слот
          </span>
          <Select value={slot} onChange={(event) => setSlot(event.target.value)}>
            {PART_SLOTS.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </Select>
        </label>
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Деталь (из владения)
          </span>
          <Select
            value={partItemId}
            onChange={(event) => setPartItemId(event.target.value)}
            required
          >
            <option value="" disabled>
              {parts.length === 0 ? "Нет деталей во владении" : "Выберите деталь"}
            </option>
            {parts.map((item) => (
              <option key={item.itemId} value={item.itemId}>
                {item.code}
              </option>
            ))}
          </Select>
        </label>
        <Button type="submit" disabled={actions.isPending || !partItemId}>
          Установить
        </Button>
      </form>
    </li>
  );
}
