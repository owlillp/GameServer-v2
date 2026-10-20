"use client";

import { adminCatalogItemsQueryOptions } from "@/src/entities/catalog";
import {
  type CreateOfferRequest,
  type Offer,
  type OfferItemRequest,
  type UpdateOfferRequest,
} from "@/src/entities/store";
import { currenciesQueryOptions } from "@/src/entities/wallet";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Input } from "@/src/shared/ui/input";
import { Select } from "@/src/shared/ui/select";
import { Spinner } from "@/src/shared/ui/spinner";
import { Textarea } from "@/src/shared/ui/textarea";
import { useQuery } from "@tanstack/react-query";
import { Plus, Trash2 } from "lucide-react";
import { useState, type FormEvent } from "react";
import { useAdminOffers } from "../model/use-admin-offers";
import {
  useActivateOffer,
  useArchiveOffer,
  useCreateOffer,
  useUpdateOffer,
} from "../model/use-offer-mutations";

export function AdminStoreManager() {
  const { offers, isPending, error } = useAdminOffers();
  const [selected, setSelected] = useState<Offer | null>(null);

  if (isPending) {
    return <Spinner label="Загружаем офферы..." />;
  }

  if (error) {
    return <ApiErrorList error={error} fallback="Не удалось загрузить офферы" />;
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)]">
      <section className="space-y-3">
        <div className="flex items-center justify-between gap-3">
          <h2 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
            Офферы
          </h2>
          <Button variant="ghost" onClick={() => setSelected(null)}>
            <Plus className="mr-1 h-4 w-4" /> Новый
          </Button>
        </div>

        {offers.length === 0 ? (
          <p className="rounded-md border border-dashed border-slate-300 px-4 py-6 text-center text-sm text-slate-500">
            Офферов пока нет.
          </p>
        ) : (
          <ul className="divide-y divide-slate-100 rounded-md border border-slate-200 bg-white">
            {offers.map((offer) => (
              <OfferRow
                key={offer.id}
                offer={offer}
                onSelect={() => setSelected(offer)}
              />
            ))}
          </ul>
        )}
      </section>

      <OfferForm
        key={selected?.id ?? "new"}
        offer={selected}
        onDone={() => setSelected(null)}
      />
    </div>
  );
}

function OfferRow({ offer, onSelect }: { offer: Offer; onSelect: () => void }) {
  const activate = useActivateOffer();
  const archive = useArchiveOffer();

  return (
    <li className="flex flex-wrap items-center justify-between gap-2 px-4 py-2.5 text-sm">
      <button
        type="button"
        onClick={onSelect}
        className="text-left font-medium text-slate-800 hover:text-slate-950"
      >
        {offer.displayName}
        <span className="ml-2 font-mono text-xs text-slate-400">{offer.code}</span>
      </button>
      <span className="flex items-center gap-2">
        <StatusBadge status={offer.status} />
        {offer.status !== "ACTIVE" && offer.status !== "ARCHIVED" && (
          <Button
            variant="ghost"
            onClick={() => activate.mutate(offer.id)}
            disabled={activate.isPending}
          >
            Активировать
          </Button>
        )}
        {offer.status !== "ARCHIVED" && (
          <Button
            variant="ghost"
            onClick={() => archive.mutate(offer.id)}
            disabled={archive.isPending}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        )}
      </span>
    </li>
  );
}

function OfferForm({ offer, onDone }: { offer: Offer | null; onDone: () => void }) {
  const isCreate = offer === null;
  const catalog = useQuery(adminCatalogItemsQueryOptions({ status: "PUBLISHED" }));
  const currencies = useQuery(currenciesQueryOptions());
  const create = useCreateOffer();
  const update = useUpdateOffer();

  const [code, setCode] = useState(offer?.code ?? "");
  const [displayName, setDisplayName] = useState(offer?.displayName ?? "");
  const [description, setDescription] = useState(offer?.description ?? "");
  const [priceCurrency, setPriceCurrency] = useState(
    offer?.priceCurrency ?? "GOLD",
  );
  const [priceAmount, setPriceAmount] = useState(
    offer ? String(offer.priceAmount) : "100",
  );
  const [sortOrder, setSortOrder] = useState(
    offer ? String(offer.sortOrder) : "0",
  );
  const [items, setItems] = useState<OfferItemRequest[]>(
    (offer?.items ?? []).map((item) => ({
      catalogItemId: item.catalogItemId,
      quantity: item.quantity,
    })),
  );
  const [selectedItemId, setSelectedItemId] = useState("");

  const mutation = isCreate ? create : update;
  const error = mutation.error;

  const addItem = () => {
    if (!selectedItemId || items.some((item) => item.catalogItemId === selectedItemId)) {
      return;
    }

    setItems((current) => [
      ...current,
      { catalogItemId: selectedItemId, quantity: 1 },
    ]);
    setSelectedItemId("");
  };

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();

    const base = {
      displayName: displayName.trim(),
      description: description.trim() || null,
      priceCurrency: priceCurrency.trim().toUpperCase(),
      priceAmount: Number(priceAmount),
      startsAt: null,
      endsAt: null,
      sortOrder: Number(sortOrder) || 0,
      items,
    };

    if (isCreate) {
      const request: CreateOfferRequest = { ...base, code: code.trim() };
      create.mutate(request, { onSuccess: onDone });
      return;
    }

    const request: UpdateOfferRequest = base;
    update.mutate({ offerId: offer.id, request }, { onSuccess: onDone });
  };

  const catalogItems = catalog.data ?? [];

  return (
    <form
      onSubmit={onSubmit}
      className="space-y-4 rounded-lg border border-slate-200 bg-white p-4"
    >
      <h2 className="text-sm font-semibold text-slate-900">
        {isCreate ? "Новый оффер" : `Оффер «${offer.displayName}»`}
      </h2>

      {error && <ApiErrorList error={error} fallback="Не удалось сохранить оффер" />}

      <div className="grid gap-3 sm:grid-cols-2">
        <Field label="Код">
          <Input
            value={code}
            onChange={(event) => setCode(event.target.value)}
            disabled={!isCreate}
            placeholder="summer-pack"
            required
          />
        </Field>
        <Field label="Название">
          <Input
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
            required
          />
        </Field>
        <Field label="Валюта">
          <Select
            value={priceCurrency}
            onChange={(event) => setPriceCurrency(event.target.value)}
          >
            {(currencies.data ?? []).length === 0 ? (
              <option value="GOLD">GOLD</option>
            ) : (
              (currencies.data ?? []).map((currency) => (
                <option key={currency.code} value={currency.code}>
                  {currency.code} — {currency.displayName}
                </option>
              ))
            )}
          </Select>
        </Field>
        <Field label="Цена">
          <Input
            type="number"
            min={1}
            value={priceAmount}
            onChange={(event) => setPriceAmount(event.target.value)}
            required
          />
        </Field>
        <Field label="Сортировка">
          <Input
            type="number"
            value={sortOrder}
            onChange={(event) => setSortOrder(event.target.value)}
          />
        </Field>
      </div>

      <Field label="Описание">
        <Textarea
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          rows={2}
        />
      </Field>

      <div className="space-y-2">
        <p className="text-xs font-medium text-slate-600">Состав оффера</p>

        {items.length === 0 ? (
          <p className="text-sm text-slate-500">Добавь хотя бы один предмет.</p>
        ) : (
          <ul className="space-y-1">
            {items.map((item) => {
              const catalogItem = catalogItems.find(
                (entry) => entry.id === item.catalogItemId,
              );

              return (
                <li
                  key={item.catalogItemId}
                  className="flex items-center justify-between gap-2 rounded border border-slate-200 px-2 py-1 text-sm"
                >
                  <span className="truncate">
                    {catalogItem?.displayName ?? item.catalogItemId}
                    <span className="ml-2 font-mono text-xs text-slate-400">
                      {catalogItem?.code}
                    </span>
                  </span>
                  <span className="flex items-center gap-1">
                    <Input
                      type="number"
                      min={1}
                      value={item.quantity}
                      onChange={(event) =>
                        setItems((current) =>
                          current.map((entry) =>
                            entry.catalogItemId === item.catalogItemId
                              ? {
                                  ...entry,
                                  quantity: Math.max(1, Number(event.target.value)),
                                }
                              : entry,
                          ),
                        )
                      }
                      className="h-7 w-16"
                    />
                    <Button
                      type="button"
                      variant="ghost"
                      onClick={() =>
                        setItems((current) =>
                          current.filter(
                            (entry) => entry.catalogItemId !== item.catalogItemId,
                          ),
                        )
                      }
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </span>
                </li>
              );
            })}
          </ul>
        )}

        <div className="flex gap-2">
          <Select
            value={selectedItemId}
            onChange={(event) => setSelectedItemId(event.target.value)}
            className="flex-1"
          >
            <option value="">Выбери предмет каталога…</option>
            {catalogItems.map((item) => (
              <option key={item.id} value={item.id}>
                {item.displayName} ({item.kind})
              </option>
            ))}
          </Select>
          <Button type="button" variant="ghost" onClick={addItem}>
            <Plus className="h-4 w-4" />
          </Button>
        </div>
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={onDone}>
          Отмена
        </Button>
        <Button
          type="submit"
          disabled={mutation.isPending || items.length === 0}
        >
          {mutation.isPending ? "Сохраняем..." : "Сохранить"}
        </Button>
      </div>
    </form>
  );
}

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block space-y-1 text-sm">
      <span className="text-xs font-medium text-slate-600">{label}</span>
      {children}
    </label>
  );
}

function StatusBadge({ status }: { status: Offer["status"] }) {
  const className =
    status === "ACTIVE"
      ? "bg-emerald-50 text-emerald-700"
      : status === "ARCHIVED"
        ? "bg-slate-100 text-slate-500"
        : "bg-amber-50 text-amber-700";

  return (
    <span className={`rounded px-2 py-0.5 text-xs font-medium ${className}`}>
      {status}
    </span>
  );
}
