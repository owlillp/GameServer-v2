"use client";

import { type CurrencyKind } from "@/src/entities/wallet";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Input } from "@/src/shared/ui/input";
import { Select } from "@/src/shared/ui/select";
import { Spinner } from "@/src/shared/ui/spinner";
import { useState, type FormEvent } from "react";
import { useCreateCurrency } from "../model/use-create-currency";
import { useCurrencies } from "../model/use-currencies";
import { useDeleteCurrency } from "../model/use-delete-currency";

const KINDS: readonly CurrencyKind[] = ["Soft", "Hard", "Event"];

export function AdminCurrencies() {
  const { currencies, isPending, error } = useCurrencies();
  const {
    createCurrency,
    isPending: isCreating,
    error: createError,
    reset: resetCreate,
  } = useCreateCurrency();
  const {
    deleteCurrency,
    isPending: isDeleting,
    variables: deletingCode,
    error: deleteError,
    reset: resetDelete,
  } = useDeleteCurrency();

  const [code, setCode] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [kind, setKind] = useState<CurrencyKind>("Soft");

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    resetCreate();

    try {
      await createCurrency({
        code: code.trim().toUpperCase(),
        displayName: displayName.trim(),
        kind,
      });
      setCode("");
      setDisplayName("");
    } catch {
      // Ошибка отображается через ApiErrorList.
    }
  };

  const onDelete = async (currencyCode: string) => {
    resetDelete();

    try {
      await deleteCurrency(currencyCode);
    } catch {
      // Ошибка отображается через ApiErrorList.
    }
  };

  return (
    <section className="space-y-4 rounded-lg border border-slate-200 bg-white p-6">
      <header className="space-y-1">
        <h2 className="text-sm font-semibold text-slate-900">Валюты</h2>
        <p className="text-xs text-slate-500">
          Динамический список доступных валют. GET /wallets/currencies,
          создание/удаление — /admin/wallets/currencies.
        </p>
      </header>

      <form onSubmit={onSubmit} className="grid gap-3 sm:grid-cols-2">
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Код
          </span>
          <Input
            name="currency-code"
            value={code}
            onChange={(event) => setCode(event.target.value)}
            placeholder="EVENT_SUMMER"
            maxLength={16}
            required
          />
        </label>

        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Название
          </span>
          <Input
            name="currency-name"
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
            placeholder="Летняя валюта"
            maxLength={64}
            required
          />
        </label>

        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Тип
          </span>
          <Select
            name="currency-kind"
            value={kind}
            onChange={(event) => setKind(event.target.value as CurrencyKind)}
          >
            {KINDS.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </Select>
        </label>

        <div className="flex items-end">
          <Button type="submit" disabled={isCreating}>
            {isCreating ? "Создаём..." : "Создать валюту"}
          </Button>
        </div>
      </form>

      {createError ? (
        <ApiErrorList error={createError} fallback="Не удалось создать валюту" />
      ) : null}

      {deleteError ? (
        <ApiErrorList error={deleteError} fallback="Не удалось удалить валюту" />
      ) : null}

      {error ? (
        <ApiErrorList error={error} fallback="Не удалось загрузить валюты" />
      ) : isPending ? (
        <Spinner label="Загружаем валюты..." />
      ) : currencies.length === 0 ? (
        <p className="text-sm text-slate-500">Валют пока нет.</p>
      ) : (
        <ul className="divide-y divide-slate-100 rounded-md border border-slate-200">
          {currencies.map((currency) => (
            <li
              key={currency.code}
              className="flex flex-wrap items-center justify-between gap-2 px-4 py-2.5 text-sm"
            >
              <span className="flex items-center gap-2">
                <code className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-xs text-slate-700">
                  {currency.code}
                </code>
                <span className="text-slate-700">{currency.displayName}</span>
                <span className="text-xs text-slate-400">{currency.kind}</span>
              </span>
              <button
                type="button"
                onClick={() => void onDelete(currency.code)}
                disabled={isDeleting}
                className="text-xs font-medium text-rose-600 underline-offset-2 hover:underline disabled:opacity-50"
              >
                {isDeleting && deletingCode === currency.code
                  ? "Удаляем..."
                  : "Удалить"}
              </button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
