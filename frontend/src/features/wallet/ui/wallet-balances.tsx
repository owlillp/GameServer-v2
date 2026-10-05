"use client";

import {
  walletQueryOptions,
  walletTransactionsQueryOptions,
  type LedgerEntry,
} from "@/src/entities/wallet";
import { ErrorType, isEnvelopeError } from "@/src/shared/api/errors";
import { cn } from "@/src/shared/lib/utils";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Spinner } from "@/src/shared/ui/spinner";
import { useQuery } from "@tanstack/react-query";

const TRANSACTIONS_LIMIT = 10;

export function WalletBalances() {
  const wallet = useQuery(walletQueryOptions());
  const transactions = useQuery(
    walletTransactionsQueryOptions(TRANSACTIONS_LIMIT),
  );

  if (wallet.isLoading) {
    return <Spinner label="Загружаем баланс..." />;
  }

  if (
    isEnvelopeError(wallet.error) &&
    wallet.error.type === ErrorType.NOT_FOUND
  ) {
    return (
      <p className="rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
        Кошелёк ещё не создан. Он появится после обработки события регистрации.
      </p>
    );
  }

  if (wallet.error) {
    return (
      <ApiErrorList error={wallet.error} fallback="Не удалось загрузить баланс" />
    );
  }

  if (!wallet.data) return null;

  const { balances } = wallet.data;

  return (
    <div className="space-y-5">
      {balances.length === 0 ? (
        <p className="rounded-md border border-dashed border-slate-300 px-4 py-6 text-center text-sm text-slate-500">
          Операций ещё не было — баланс пуст.
        </p>
      ) : (
        <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {balances.map((balance) => (
            <li
              key={balance.currency}
              className="rounded-lg border border-slate-200 bg-white p-4"
            >
              <p className="text-xs font-medium tracking-wide text-slate-500 uppercase">
                {balance.currency}
              </p>
              <p className="mt-1 text-2xl font-semibold text-slate-900 tabular-nums">
                {formatAmount(balance.available)}
              </p>
            </li>
          ))}
        </ul>
      )}

      <div className="space-y-2">
        <h3 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
          Последние операции
        </h3>

        {transactions.error ? (
          <ApiErrorList
            error={transactions.error}
            fallback="Не удалось загрузить историю"
          />
        ) : transactions.isLoading ? (
          <Spinner label="Загружаем историю..." />
        ) : (transactions.data ?? []).length === 0 ? (
          <p className="text-sm text-slate-500">Операций пока нет.</p>
        ) : (
          <ul className="divide-y divide-slate-100 rounded-md border border-slate-200 bg-white">
            {(transactions.data ?? []).map((entry) => (
              <TransactionRow key={entry.id} entry={entry} />
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function TransactionRow({ entry }: { entry: LedgerEntry }) {
  const isTopUp = entry.direction === "TOP_UP";

  return (
    <li className="flex flex-wrap items-center justify-between gap-2 px-4 py-2.5 text-sm">
      <span className="flex items-center gap-2">
        <span
          className={cn(
            "inline-flex h-6 min-w-16 items-center justify-center rounded px-2 text-xs font-medium",
            isTopUp
              ? "bg-emerald-50 text-emerald-700"
              : "bg-rose-50 text-rose-700",
          )}
        >
          {isTopUp ? "+" : "−"}
          {formatAmount(entry.amount)} {entry.currency}
        </span>
        <span className="text-slate-500">{entry.reason}</span>
      </span>
      <span className="text-xs text-slate-400">{formatDate(entry.createdAt)}</span>
    </li>
  );
}

function formatAmount(value: number): string {
  return value.toLocaleString("ru-RU");
}

function formatDate(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;

  return date.toLocaleString("ru-RU", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}
