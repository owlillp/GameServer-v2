"use client";

import {
  myOrdersQueryOptions,
  type Offer,
  type Order,
  type OrderStatus,
} from "@/src/entities/store";
import { inventoryQueryKeys } from "@/src/entities/inventory";
import { walletQueryKeys } from "@/src/entities/wallet";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Spinner } from "@/src/shared/ui/spinner";
import { useQueryClient } from "@tanstack/react-query";
import { CheckCircle2, Coins, ShoppingBag, XCircle } from "lucide-react";
import { useEffect, useState } from "react";
import { useMyOrders } from "../model/use-my-orders";
import { useOrderStatus } from "../model/use-order-status";
import { usePurchaseOffer } from "../model/use-purchase-offer";
import { useStoreOffers } from "../model/use-store-offers";

const PENDING_STATUSES: OrderStatus[] = ["PENDING", "RESERVED", "GRANT_REQUESTED"];

export function StoreView() {
  const { offers, isPending, error } = useStoreOffers();
  const { orders } = useMyOrders();
  const purchase = usePurchaseOffer();
  const [activeOrderId, setActiveOrderId] = useState<string | null>(null);
  const orderStatus = useOrderStatus(activeOrderId);
  const queryClient = useQueryClient();

  const activeOrder = orderStatus.order ?? purchase.data ?? null;

  useEffect(() => {
    if (activeOrder?.status !== "COMPLETED") return;

    void queryClient.invalidateQueries({ queryKey: myOrdersQueryOptions().queryKey });
    void queryClient.invalidateQueries({ queryKey: walletQueryKeys.me() });
    void queryClient.invalidateQueries({ queryKey: inventoryQueryKeys.me() });
  }, [activeOrder?.status, queryClient]);

  const handlePurchase = (offerId: string) => {
    purchase.mutate(offerId, {
      onSuccess: (order) => setActiveOrderId(order.id),
    });
  };

  if (isPending) {
    return <Spinner label="Загружаем витрину..." />;
  }

  if (error) {
    return <ApiErrorList error={error} fallback="Не удалось загрузить магазин" />;
  }

  return (
    <div className="space-y-6">
      {activeOrder && (
        <OrderProgress
          order={activeOrder}
          isLoading={orderStatus.isPending && PENDING_STATUSES.includes(activeOrder.status)}
        />
      )}

      {purchase.error && (
        <ApiErrorList error={purchase.error} fallback="Не удалось оформить покупку" />
      )}

      {offers.length === 0 ? (
        <p className="rounded-md border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500">
          Витрина пока пуста. Администратор ещё не опубликовал офферы.
        </p>
      ) : (
        <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {offers.map((offer) => (
            <OfferCard
              key={offer.id}
              offer={offer}
              isBuying={purchase.isPending && purchase.variables === offer.id}
              onBuy={() => handlePurchase(offer.id)}
            />
          ))}
        </ul>
      )}

      <OrderHistory orders={orders} />
    </div>
  );
}

function OfferCard({
  offer,
  isBuying,
  onBuy,
}: {
  offer: Offer;
  isBuying: boolean;
  onBuy: () => void;
}) {
  return (
    <li className="flex flex-col rounded-lg border border-slate-200 bg-white p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-sm font-semibold text-slate-900">
            {offer.displayName}
          </h2>
          <p className="mt-1 text-xs text-slate-500">{offer.code}</p>
        </div>
        <span className="inline-flex items-center gap-1 rounded-md bg-amber-50 px-2 py-1 text-xs font-semibold text-amber-700">
          <Coins className="h-3.5 w-3.5" />
          {formatAmount(offer.priceAmount)} {offer.priceCurrency}
        </span>
      </div>

      {offer.description && (
        <p className="mt-3 text-sm text-slate-600">{offer.description}</p>
      )}

      <ul className="mt-3 space-y-1 text-sm text-slate-600">
        {offer.items.map((item) => (
          <li key={item.catalogItemId} className="flex justify-between gap-2">
            <span className="font-mono text-xs text-slate-500">{item.code}</span>
            <span className="text-xs">
              {KIND_LABEL[item.kind] ?? item.kind}
              {item.quantity > 1 ? ` ×${item.quantity}` : ""}
            </span>
          </li>
        ))}
      </ul>

      <div className="mt-4 flex-1" />
      <Button onClick={onBuy} disabled={isBuying} className="w-full">
        {isBuying ? "Оформляем..." : "Купить"}
      </Button>
    </li>
  );
}

function OrderProgress({
  order,
  isLoading,
}: {
  order: Order;
  isLoading: boolean;
}) {
  if (order.status === "COMPLETED") {
    return (
      <div className="flex items-start gap-3 rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
        <CheckCircle2 className="mt-0.5 h-4 w-4" />
        <div>
          <p className="font-medium">Покупка «{order.offerName}» завершена</p>
          <p className="text-xs text-emerald-700">
            Предметы добавлены в инвентарь
            {order.completedAt ? ` · ${formatDate(order.completedAt)}` : ""}
          </p>
        </div>
      </div>
    );
  }

  if (order.status === "FAILED") {
    return (
      <div className="flex items-start gap-3 rounded-md border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">
        <XCircle className="mt-0.5 h-4 w-4" />
        <div>
          <p className="font-medium">Покупка не выполнена</p>
          <p className="text-xs text-rose-700">
            {order.failureReason ?? "Не удалось завершить операцию"}
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex items-start gap-3 rounded-md border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-800">
      <ShoppingBag className="mt-0.5 h-4 w-4" />
      <div>
        <p className="font-medium">
          Обрабатываем покупку «{order.offerName}»
        </p>
        <p className="text-xs text-sky-700">
          {isLoading ? "Проверяем статус..." : "Выдача предметов занимает несколько секунд."}
        </p>
      </div>
    </div>
  );
}

function OrderHistory({ orders }: { orders: Order[] }) {
  if (orders.length === 0) {
    return null;
  }

  return (
    <section className="space-y-2">
      <h2 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
        История покупок
      </h2>
      <ul className="divide-y divide-slate-100 rounded-md border border-slate-200 bg-white">
        {orders.slice(0, 10).map((order) => (
          <li
            key={order.id}
            className="flex flex-wrap items-center justify-between gap-2 px-4 py-2.5 text-sm"
          >
            <span className="font-medium text-slate-700">{order.offerName}</span>
            <span className="flex items-center gap-3 text-xs text-slate-500">
              <span>{formatAmount(order.totalAmount)} {order.currency}</span>
              <StatusBadge status={order.status} />
              <span>{formatDate(order.createdAt)}</span>
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
}

function StatusBadge({ status }: { status: OrderStatus }) {
  const className =
    status === "COMPLETED"
      ? "bg-emerald-50 text-emerald-700"
      : status === "FAILED"
        ? "bg-rose-50 text-rose-700"
        : "bg-sky-50 text-sky-700";

  return (
    <span className={`rounded px-2 py-0.5 font-medium ${className}`}>
      {STATUS_LABEL[status]}
    </span>
  );
}

const KIND_LABEL: Record<string, string> = {
  CLOTHING: "Одежда",
  CAR: "Машина",
  CAR_PART: "Деталь",
};

const STATUS_LABEL: Record<OrderStatus, string> = {
  PENDING: "Создан",
  RESERVED: "Средства зарезервированы",
  GRANT_REQUESTED: "Выдаём предметы",
  COMPLETED: "Готово",
  FAILED: "Ошибка",
};

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
