"use client";

import { routes } from "@/src/shared/routes";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Input } from "@/src/shared/ui/input";
import { Select } from "@/src/shared/ui/select";
import { Textarea } from "@/src/shared/ui/textarea";
import Link from "next/link";
import { useState, type FormEvent } from "react";
import {
  useAdminWalletOperation,
  type AdminWalletOperationType,
} from "../model/use-admin-wallet-operation";
import { useCurrencies } from "../model/use-currencies";

const REASONS = [
  { value: "AdminAdjustment", label: "Админ-корректировка" },
  { value: "Compensation", label: "Компенсация" },
  { value: "Refund", label: "Возврат" },
] as const;

const OPERATIONS: readonly {
  value: AdminWalletOperationType;
  label: string;
}[] = [
  { value: "top-up", label: "Начислить" },
  { value: "spent", label: "Списать" },
];

type Props = {
  // Если задан — операция выполняется для этого кошелька (карточка игрока),
  // поле ввода ID скрывается.
  fixedWalletId?: string;
};

export function AdminWalletAdjustment({ fixedWalletId }: Props) {
  const { currencies } = useCurrencies();
  const { run, isPending, error, result, reset } = useAdminWalletOperation();

  const [walletId, setWalletId] = useState(fixedWalletId ?? "");
  const [type, setType] = useState<AdminWalletOperationType>("top-up");
  const [currency, setCurrency] = useState("");
  const [amount, setAmount] = useState("");
  const [reason, setReason] = useState<string>(REASONS[0].value);
  const [comment, setComment] = useState("");
  const [referenceId, setReferenceId] = useState("");

  const targetWalletId = fixedWalletId ?? walletId.trim();
  const parsedAmount = Number(amount);
  const isValid =
    targetWalletId.length > 0 &&
    currency.length > 0 &&
    Number.isInteger(parsedAmount) &&
    parsedAmount > 0 &&
    comment.trim().length > 0;

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    reset();

    if (!isValid) return;

    try {
      await run({
        type,
        walletId: targetWalletId,
        request: {
          currency,
          amount: parsedAmount,
          reason,
          comment: comment.trim(),
          // Ключ идемпотентности на один сабмит — защищает от двойного клика.
          operationId: crypto.randomUUID(),
          referenceId: referenceId.trim() || null,
        },
      });
      setAmount("");
      setComment("");
      setReferenceId("");
    } catch {
      // Ошибка отображается через ApiErrorList.
    }
  };

  return (
    <section className="space-y-4 rounded-lg border border-slate-200 bg-white p-6">
      <header className="space-y-1">
        <h2 className="text-sm font-semibold text-slate-900">
          Ручная операция
        </h2>
        <p className="text-xs text-slate-500">
          Начисление/списание игроку. POST /admin/wallets/{"{id}"}/top-up|spent
          (permission wallets.manage, обязательный comment).
        </p>
      </header>

      <form onSubmit={onSubmit} className="grid gap-3 sm:grid-cols-2">
        {fixedWalletId ? (
          <p className="text-xs text-slate-500 sm:col-span-2">
            Кошелёк:{" "}
            <code className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-slate-700">
              {fixedWalletId}
            </code>
          </p>
        ) : (
          <label className="block sm:col-span-2">
            <span className="mb-1 block text-xs font-medium text-slate-600">
              ID кошелька игрока (account_id)
            </span>
            <Input
              name="walletId"
              value={walletId}
              onChange={(event) => setWalletId(event.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
              required
            />
          </label>
        )}

        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Операция
          </span>
          <Select
            name="operation"
            value={type}
            onChange={(event) =>
              setType(event.target.value as AdminWalletOperationType)
            }
          >
            {OPERATIONS.map((operation) => (
              <option key={operation.value} value={operation.value}>
                {operation.label}
              </option>
            ))}
          </Select>
        </label>

        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Валюта
          </span>
          <Select
            name="currency"
            value={currency}
            onChange={(event) => setCurrency(event.target.value)}
            required
          >
            <option value="" disabled>
              Выберите валюту
            </option>
            {currencies.map((item) => (
              <option key={item.code} value={item.code}>
                {item.code} — {item.displayName}
              </option>
            ))}
          </Select>
        </label>

        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Сумма (целое, минорные единицы)
          </span>
          <Input
            name="amount"
            type="number"
            inputMode="numeric"
            step={1}
            min={1}
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            placeholder="1000"
            required
          />
        </label>

        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Причина
          </span>
          <Select
            name="reason"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
          >
            {REASONS.map((item) => (
              <option key={item.value} value={item.value}>
                {item.label}
              </option>
            ))}
          </Select>
        </label>

        <label className="block sm:col-span-2">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Комментарий (аудит)
          </span>
          <Textarea
            name="comment"
            value={comment}
            onChange={(event) => setComment(event.target.value)}
            rows={2}
            maxLength={512}
            placeholder="Тикет #1234: компенсация за сбой"
            required
          />
        </label>

        <label className="block sm:col-span-2">
          <span className="mb-1 block text-xs font-medium text-slate-600">
            Reference (необязательно)
          </span>
          <Input
            name="referenceId"
            value={referenceId}
            onChange={(event) => setReferenceId(event.target.value)}
            maxLength={64}
            placeholder="ticket:1234"
          />
        </label>

        <div>
          <Button type="submit" disabled={isPending || !isValid}>
            {isPending ? "Выполняем..." : "Выполнить"}
          </Button>
        </div>
      </form>

      {error ? (
        <ApiErrorList error={error} fallback="Не удалось выполнить операцию" />
      ) : null}

      {result ? (
        <div className="space-y-2 rounded-md border border-emerald-200 bg-emerald-50 p-4">
          <p className="text-xs font-medium text-emerald-800">
            Баланс кошелька {result.walletId}
          </p>
          <ul className="flex flex-wrap gap-3 text-sm text-emerald-900">
            {result.balances.map((balance) => (
              <li key={balance.currency}>
                {balance.currency}:{" "}
                <span className="font-semibold tabular-nums">
                  {balance.available.toLocaleString("ru-RU")}
                </span>
              </li>
            ))}
          </ul>
          <Link
            href={routes.adminPlayer(result.walletId)}
            className="inline-flex text-xs font-medium text-emerald-800 underline underline-offset-2"
          >
            Открыть карточку игрока
          </Link>
        </div>
      ) : null}
    </section>
  );
}
