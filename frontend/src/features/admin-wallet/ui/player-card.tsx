"use client";

import type { LedgerEntry } from "@/src/entities/wallet";
import { ErrorType, isEnvelopeError } from "@/src/shared/api/errors";
import { cn } from "@/src/shared/lib/utils";
import { routes } from "@/src/shared/routes";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Spinner } from "@/src/shared/ui/spinner";
import Link from "next/link";
import { useAdminWallet } from "../model/use-admin-wallet";
import { useAdminWalletTransactions } from "../model/use-admin-wallet-transactions";
import { AdminWalletAdjustment } from "./admin-wallet-adjustment";

const TRANSACTIONS_LIMIT = 50;

export function PlayerCard({ walletId }: { walletId: string }) {
  const { wallet, isPending, error } = useAdminWallet(walletId);
  const {
    transactions,
    isPending: transactionsPending,
    error: transactionsError,
  } = useAdminWalletTransactions(walletId, TRANSACTIONS_LIMIT);

  if (isPending) {
    return <Spinner label="Загружаем карточку игрока..." />;
  }

  if (isEnvelopeError(error) && error.type === ErrorType.NOT_FOUND) {
    return (
      <p className="rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
        Кошелёк не найден. Возможно, игрок ещё не зарегистрирован.
      </p>
    );
  }

  if (error) {
    return (
      <ApiErrorList error={error} fallback="Не удалось загрузить игрока" />
    );
  }

  if (!wallet) return null;

  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <div className="space-y-6">
        <section className="space-y-3 rounded-lg border border-slate-200 bg-white p-6">
          <h2 className="text-sm font-semibold text-slate-900">Игрок</h2>
          <dl className="space-y-2 text-sm">
            <div className="flex flex-wrap justify-between gap-2">
              <dt className="text-slate-500">Кошелёк / account_id</dt>
              <dd className="font-mono text-xs break-all text-slate-900">
                {wallet.walletId}
              </dd>
            </div>
            <div className="flex flex-wrap justify-between gap-2">
              <dt className="text-slate-500">Создан</dt>
              <dd className="text-slate-900">{formatDate(wallet.createdAt)}</dd>
            </div>
            <div className="flex flex-wrap justify-between gap-2">
              <dt className="text-slate-500">Обновлён</dt>
              <dd className="text-slate-900">{formatDate(wallet.updatedAt)}</dd>
            </div>
          </dl>

          <Link
            href={routes.adminUsers}
            className="inline-flex text-xs font-medium text-slate-500 underline underline-offset-2"
          >
            ← К списку пользователей
          </Link>
        </section>

        <section className="space-y-3 rounded-lg border border-slate-200 bg-white p-6">
          <h2 className="text-sm font-semibold text-slate-900">Балансы</h2>
          {wallet.balances.length === 0 ? (
            <p className="text-sm text-slate-500">Баланс пуст.</p>
          ) : (
            <ul className="grid gap-3 sm:grid-cols-2">
              {wallet.balances.map((balance) => (
                <li
                  key={balance.currency}
                  className="rounded-md border border-slate-200 p-3"
                >
                  <p className="text-xs font-medium tracking-wide text-slate-500 uppercase">
                    {balance.currency}
                  </p>
                  <p className="mt-1 text-xl font-semibold text-slate-900 tabular-nums">
                    {formatAmount(balance.available)}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="space-y-3 rounded-lg border border-slate-200 bg-white p-6">
          <h2 className="text-sm font-semibold text-slate-900">
            История операций
          </h2>
          {transactionsError ? (
            <ApiErrorList
              error={transactionsError}
              fallback="Не удалось загрузить историю"
            />
          ) : transactionsPending ? (
            <Spinner label="Загружаем историю..." />
          ) : transactions.length === 0 ? (
            <p className="text-sm text-slate-500">Операций пока нет.</p>
          ) : (
            <ul className="divide-y divide-slate-100 rounded-md border border-slate-200">
              {transactions.map((entry) => (
                <TransactionRow key={entry.id} entry={entry} />
              ))}
            </ul>
          )}
        </section>
      </div>

      <AdminWalletAdjustment fixedWalletId={walletId} />
    </div>
  );
}

function TransactionRow({ entry }: { entry: LedgerEntry }) {
  const isCredit = entry.direction === "Credit";

  return (
    <li className="space-y-1 px-4 py-2.5 text-sm">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span
          className={cn(
            "inline-flex h-6 min-w-16 items-center justify-center rounded px-2 text-xs font-medium",
            isCredit
              ? "bg-emerald-50 text-emerald-700"
              : "bg-rose-50 text-rose-700",
          )}
        >
          {isCredit ? "+" : "−"}
          {formatAmount(entry.amount)} {entry.currency}
        </span>
        <span className="text-xs text-slate-400">
          {formatDate(entry.createdAt)}
        </span>
      </div>
      <p className="text-xs text-slate-500">
        {entry.reason}
        {entry.actorType ? ` · ${entry.actorType}` : ""}
      </p>
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
