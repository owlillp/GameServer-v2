"use client";

import type {
  ClaimStatus,
  EventDetails,
  EventPhase,
  EventReward,
  GameEvent,
  QuestMetric,
} from "@/src/entities/events";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Spinner } from "@/src/shared/ui/spinner";
import { CalendarDays, Coins, Gift, Sparkles, Trophy } from "lucide-react";
import { useState } from "react";
import { useClaimReward } from "../model/use-claim-reward";
import { useEventDetails } from "../model/use-event-details";
import { useEvents } from "../model/use-events";

export function EventsView() {
  const { events, isPending, error } = useEvents();
  const [selectedCode, setSelectedCode] = useState<string | null>(null);
  const defaultCode =
    events.find((event) => event.phase === "ACTIVE")?.code ??
    events[0]?.code ??
    null;
  const code = selectedCode ?? defaultCode;
  const details = useEventDetails(code);
  const claim = useClaimReward();

  if (isPending) {
    return <Spinner label="Загружаем ивенты..." />;
  }

  if (error) {
    return (
      <ApiErrorList error={error} fallback="Не удалось загрузить ивенты" />
    );
  }

  if (events.length === 0) {
    return (
      <p className="rounded-md border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500">
        Сейчас нет активных ивентов. Загляни позже.
      </p>
    );
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,18rem)_minmax(0,1fr)]">
      <section className="space-y-2">
        <h2 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
          Ивенты
        </h2>
        <ul className="space-y-2">
          {events.map((event) => (
            <EventRow
              key={event.id}
              event={event}
              active={event.code === code}
              onSelect={() => setSelectedCode(event.code)}
            />
          ))}
        </ul>
      </section>

      <section className="min-w-0">
        {details.isPending ? (
          <Spinner label="Загружаем ивент..." />
        ) : details.error ? (
          <ApiErrorList
            error={details.error}
            fallback="Не удалось загрузить ивент"
          />
        ) : details.details ? (
          <EventDetailsPanel details={details.details} claim={claim} />
        ) : null}
      </section>
    </div>
  );
}

function EventRow({
  event,
  active,
  onSelect,
}: {
  event: GameEvent;
  active: boolean;
  onSelect: () => void;
}) {
  return (
    <li>
      <button
        type="button"
        onClick={onSelect}
        aria-current={active ? "true" : undefined}
        className={`w-full space-y-1 rounded-lg border px-4 py-3 text-left transition-colors ${
          active
            ? "border-slate-900 bg-slate-50"
            : "border-slate-200 bg-white hover:border-slate-300"
        }`}
      >
        <span className="flex items-center justify-between gap-2">
          <span className="font-medium text-slate-900">
            {event.displayName}
          </span>
          <PhaseBadge phase={event.phase} />
        </span>
        <span className="flex items-center gap-1 text-xs text-slate-500">
          <CalendarDays className="h-3.5 w-3.5" />
          {formatDate(event.startsAt)} — {formatDate(event.endsAt)}
        </span>
      </button>
    </li>
  );
}

function EventDetailsPanel({
  details,
  claim,
}: {
  details: EventDetails;
  claim: ReturnType<typeof useClaimReward>;
}) {
  const { event, progress, claims } = details;
  const claimedRewardIds = new Set(
    claims
      .filter((entry) => entry.status !== "FAILED")
      .map((entry) => entry.rewardId),
  );
  const claimable = event.phase === "ACTIVE" || event.phase === "GRACE";

  return (
    <div className="space-y-6">
      <header className="space-y-2 rounded-lg border border-slate-200 bg-white p-5">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div>
            <h2 className="text-xl font-semibold text-slate-900">
              {event.displayName}
            </h2>
            <p className="font-mono text-xs text-slate-400">{event.code}</p>
          </div>
          <div className="flex items-center gap-2">
            <PhaseBadge phase={event.phase} />
            <span className="inline-flex items-center gap-1 rounded-md bg-amber-50 px-2 py-1 text-sm font-semibold text-amber-700">
              <Coins className="h-4 w-4" />
              {progress.pointsBalance} {event.currencyCode}
            </span>
          </div>
        </div>

        {event.description && (
          <p className="text-sm text-slate-600">{event.description}</p>
        )}

        <dl className="grid gap-3 text-sm sm:grid-cols-3">
          <Info label="Начало" value={formatDate(event.startsAt)} />
          <Info label="Конец" value={formatDate(event.endsAt)} />
          <Info
            label="Всего заработано"
            value={String(progress.totalPointsEarned)}
          />
        </dl>
      </header>

      <section className="space-y-3">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-slate-900">
          <Trophy className="h-4 w-4 text-slate-500" /> Задания
        </h3>

        {event.quests.length === 0 ? (
          <p className="text-sm text-slate-500">У ивента пока нет заданий.</p>
        ) : (
          <ul className="space-y-2">
            {event.quests.map((quest) => {
              const questProgress = progress.quests.find(
                (entry) => entry.questId === quest.id,
              );
              const current = questProgress?.progress ?? 0;
              const percent = Math.min(
                100,
                Math.round((current / quest.target) * 100),
              );
              const completed = questProgress?.isCompleted ?? false;

              return (
                <li
                  key={quest.id}
                  className="rounded-lg border border-slate-200 bg-white p-4"
                >
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <div>
                      <p className="text-sm font-medium text-slate-900">
                        {quest.displayName}
                      </p>
                      <p className="text-xs text-slate-500">
                        {metricLabel(quest.metric)} · {quest.kind}
                      </p>
                    </div>
                    <span className="inline-flex items-center gap-1 text-xs font-semibold text-amber-700">
                      <Sparkles className="h-3.5 w-3.5" /> +{quest.points}
                    </span>
                  </div>

                  <div className="mt-3 h-2 overflow-hidden rounded-full bg-slate-100">
                    <div
                      className={`h-full rounded-full ${
                        completed ? "bg-emerald-500" : "bg-slate-900"
                      }`}
                      style={{ width: `${percent}%` }}
                    />
                  </div>
                  <p className="mt-1 text-xs text-slate-500">
                    {current} / {quest.target}
                    {completed ? " · выполнено" : ""}
                  </p>
                </li>
              );
            })}
          </ul>
        )}
      </section>

      <section className="space-y-3">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-slate-900">
          <Gift className="h-4 w-4 text-slate-500" /> Награды
        </h3>

        {claim.error && (
          <ApiErrorList
            error={claim.error}
            fallback="Не удалось получить награду"
          />
        )}

        {event.rewards.length === 0 ? (
          <p className="text-sm text-slate-500">Награды ещё не настроены.</p>
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {event.rewards.map((reward) => (
              <RewardCard
                key={reward.id}
                reward={reward}
                currency={event.currencyCode}
                balance={progress.pointsBalance}
                claimable={claimable}
                claimed={claimedRewardIds.has(reward.id)}
                isClaiming={
                  claim.isPending && claim.variables?.rewardCode === reward.code
                }
                onClaim={() =>
                  claim.mutate({
                    eventCode: event.code,
                    rewardCode: reward.code,
                  })
                }
              />
            ))}
          </ul>
        )}
      </section>

      <ClaimsHistory claims={claims} />
    </div>
  );
}

function RewardCard({
  reward,
  currency,
  balance,
  claimable,
  claimed,
  isClaiming,
  onClaim,
}: {
  reward: EventReward;
  currency: string;
  balance: number;
  claimable: boolean;
  claimed: boolean;
  isClaiming: boolean;
  onClaim: () => void;
}) {
  const notEnough = balance < reward.pointsCost;
  const disabled = !claimable || notEnough || claimed || isClaiming;

  return (
    <li className="flex flex-col rounded-lg border border-slate-200 bg-white p-4">
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="text-sm font-semibold text-slate-900">
            {reward.displayName}
          </p>
          <p className="font-mono text-xs text-slate-400">{reward.itemCode}</p>
        </div>
        <span className="inline-flex items-center gap-1 text-sm font-semibold text-amber-700">
          <Coins className="h-4 w-4" /> {reward.pointsCost}
        </span>
      </div>

      {reward.description && (
        <p className="mt-2 text-sm text-slate-600">{reward.description}</p>
      )}

      <p className="mt-2 text-xs text-slate-500">
        {reward.itemKind}
        {reward.slot ? ` · ${reward.slot}` : ""}
        {reward.quantity > 1 ? ` · ×${reward.quantity}` : ""}
        {reward.isRepeatable ? " · повторяемая" : ""}
      </p>

      <div className="mt-4 flex-1" />
      <Button onClick={onClaim} disabled={disabled} className="w-full">
        {isClaiming
          ? "Получаем..."
          : claimed
            ? "Уже получено"
            : notEnough
              ? `Нужно ${reward.pointsCost} ${currency}`
              : claimable
                ? "Забрать"
                : "Недоступно"}
      </Button>
    </li>
  );
}

function ClaimsHistory({ claims }: { claims: EventDetails["claims"] }) {
  if (claims.length === 0) {
    return null;
  }

  return (
    <section className="space-y-2">
      <h3 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
        История наград
      </h3>
      <ul className="divide-y divide-slate-100 rounded-md border border-slate-200 bg-white">
        {claims.slice(0, 10).map((entry) => (
          <li
            key={entry.id}
            className="flex flex-wrap items-center justify-between gap-2 px-4 py-2.5 text-sm"
          >
            <span className="font-mono text-xs text-slate-700">
              {entry.rewardCode}
            </span>
            <span className="flex items-center gap-3 text-xs text-slate-500">
              <span>{entry.pointsSpent}</span>
              <ClaimBadge status={entry.status} />
              <span>{formatDate(entry.createdAt)}</span>
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
}

function PhaseBadge({ phase }: { phase: EventPhase }) {
  const className =
    phase === "ACTIVE"
      ? "bg-emerald-50 text-emerald-700"
      : phase === "GRACE"
        ? "bg-amber-50 text-amber-700"
        : phase === "ANNOUNCED"
          ? "bg-sky-50 text-sky-700"
          : phase === "DRAFT"
            ? "bg-slate-100 text-slate-600"
            : "bg-slate-100 text-slate-500";

  return (
    <span className={`rounded px-2 py-0.5 text-xs font-medium ${className}`}>
      {PHASE_LABEL[phase]}
    </span>
  );
}

function ClaimBadge({ status }: { status: ClaimStatus }) {
  const className =
    status === "COMPLETED"
      ? "bg-emerald-50 text-emerald-700"
      : status === "FAILED"
        ? "bg-rose-50 text-rose-700"
        : "bg-sky-50 text-sky-700";

  return (
    <span className={`rounded px-2 py-0.5 font-medium ${className}`}>
      {CLAIM_LABEL[status]}
    </span>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-md bg-slate-50 px-3 py-2">
      <dt className="text-xs text-slate-500">{label}</dt>
      <dd className="font-medium text-slate-900">{value}</dd>
    </div>
  );
}

const PHASE_LABEL: Record<EventPhase, string> = {
  DRAFT: "Черновик",
  SCHEDULED: "Запланирован",
  ANNOUNCED: "Анонс",
  ACTIVE: "Активен",
  GRACE: "Завершение",
  CLOSED: "Закрыт",
};

const CLAIM_LABEL: Record<ClaimStatus, string> = {
  PENDING: "Выдаётся",
  COMPLETED: "Получено",
  FAILED: "Ошибка",
};

function metricLabel(metric: QuestMetric): string {
  switch (metric) {
    case "MATCHES":
      return "Матчи";
    case "WINS":
      return "Победы";
    case "KILLS":
      return "Убийства";
    case "SCORE":
      return "Очки";
    default:
      return metric;
  }
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
