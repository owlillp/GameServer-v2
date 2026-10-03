"use client";

import { cn } from "@/src/shared/lib/utils";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Spinner } from "@/src/shared/ui/spinner";
import { Lock, Shield, ShieldCheck, Users } from "lucide-react";
import { useAdminStats } from "../model/use-admin-stats";

export function AdminStatsCards() {
  const { stats, isPending, isFetching, error } = useAdminStats();

  if (error) {
    return (
      <ApiErrorList error={error} fallback="Не удалось загрузить статистику" />
    );
  }

  if (isPending || !stats) {
    return <Spinner label="Загружаем статистику..." />;
  }

  const cards = [
    {
      label: "Всего пользователей",
      value: stats.totalUsers,
      icon: Users,
      tone: "text-slate-700 bg-slate-100",
    },
    {
      label: "Админов",
      value: stats.adminCount,
      icon: ShieldCheck,
      tone: "text-rose-700 bg-rose-50",
    },
    {
      label: "Модераторов",
      value: stats.moderatorCount,
      icon: Shield,
      tone: "text-amber-700 bg-amber-50",
    },
    {
      label: "Заблокировано",
      value: stats.lockedOutCount,
      icon: Lock,
      tone: "text-slate-700 bg-slate-100",
    },
  ];

  return (
    <div
      aria-busy={isFetching}
      className={cn(
        "grid gap-4 sm:grid-cols-2 lg:grid-cols-4",
        isFetching && "opacity-60",
      )}
    >
      {cards.map((card) => (
        <section
          key={card.label}
          className="rounded-lg border border-slate-200 bg-white p-4"
        >
          <div className="flex items-center justify-between gap-3">
            <h2 className="text-xs font-medium tracking-wide text-slate-500 uppercase">
              {card.label}
            </h2>
            <span
              aria-hidden="true"
              className={cn("rounded-md p-1.5", card.tone)}
            >
              <card.icon className="h-4 w-4" />
            </span>
          </div>
          <p className="mt-2 text-2xl font-semibold text-slate-900 tabular-nums">
            {card.value}
          </p>
        </section>
      ))}
    </div>
  );
}
