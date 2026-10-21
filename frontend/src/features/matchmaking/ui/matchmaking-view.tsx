"use client";

import {
  myMatchQueryOptions,
  type LeaderboardEntry,
  type MatchDetails,
  type Party,
  type PartyState,
} from "@/src/entities/matchmaking";
import { sessionSelectors, useSessionStore } from "@/src/shared/stores/session-store";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Input } from "@/src/shared/ui/input";
import { Spinner } from "@/src/shared/ui/spinner";
import {
  Check,
  Copy,
  Crown,
  LogOut,
  Search,
  Swords,
  Trophy,
  UserPlus,
  Users,
} from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useLeaderboard } from "../model/use-leaderboard";
import { useMyParty } from "../model/use-my-party";
import { usePartyActions } from "../model/use-party-actions";

export function MatchmakingView() {
  const { state, isPending, error } = useMyParty();
  const actions = usePartyActions();
  const user = useSessionStore(sessionSelectors.user);
  const accountId = user?.sub ?? "";
  const [joinCode, setJoinCode] = useState("");

  if (isPending) {
    return <Spinner label="Загружаем пати..." />;
  }

  if (error) {
    return <ApiErrorList error={error} fallback="Не удалось загрузить пати" />;
  }

  const actionError =
    actions.create.error ??
    actions.join.error ??
    actions.leave.error ??
    actions.setReady.error ??
    actions.kick.error ??
    actions.enqueue.error ??
    actions.leaveQueue.error;

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,20rem)]">
      <section className="space-y-6">
        {actionError && (
          <ApiErrorList error={actionError} fallback="Действие не выполнено" />
        )}

        {state?.party ? (
          <PartyPanel
            state={state}
            accountId={accountId}
            actions={actions}
          />
        ) : (
          <div className="space-y-4">
            <section className="space-y-3 rounded-lg border border-slate-200 bg-white p-5">
              <h2 className="flex items-center gap-2 text-sm font-semibold text-slate-900">
                <Users className="h-4 w-4 text-slate-500" />
                Создать пати
              </h2>
              <p className="text-sm text-slate-600">
                Соберите 2–5 игроков, отметьте готовность и встаньте в очередь.
              </p>
              <Button
                onClick={() => actions.create.mutate()}
                disabled={actions.isPending}
              >
                Создать пати
              </Button>
            </section>

            <section className="space-y-3 rounded-lg border border-slate-200 bg-white p-5">
              <h2 className="flex items-center gap-2 text-sm font-semibold text-slate-900">
                <UserPlus className="h-4 w-4 text-slate-500" />
                Войти по коду
              </h2>
              <div className="flex flex-wrap gap-2">
                <Input
                  value={joinCode}
                  onChange={(event) => setJoinCode(event.target.value)}
                  placeholder="Например, A1B2C3D4"
                  className="max-w-xs font-mono uppercase"
                  maxLength={8}
                />
                <Button
                  onClick={() => actions.join.mutate(joinCode.trim())}
                  disabled={actions.isPending || joinCode.trim().length < 4}
                >
                  Войти
                </Button>
              </div>
            </section>
          </div>
        )}

        {state?.activeMatch && <ActiveMatch />}
      </section>

      <LeaderboardPanel />
    </div>
  );
}

function PartyPanel({
  state,
  accountId,
  actions,
}: {
  state: PartyState;
  accountId: string;
  actions: ReturnType<typeof usePartyActions>;
}) {
  const [copied, setCopied] = useState(false);
  const party = state.party;
  if (!party) {
    return null;
  }

  const isLeader = party.leaderAccountId === accountId;
  const me = party.members.find((member) => member.accountId === accountId);
  const allReady = party.members.every((member) => member.isReady);
  const canQueue =
    party.status === "FORMING" &&
    party.size >= 2 &&
    party.size <= 5 &&
    allReady;
  const inQueue = Boolean(state.ticket);

  const copyCode = async () => {
    try {
      await navigator.clipboard.writeText(party.code);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1500);
    } catch {
      setCopied(false);
    }
  };

  return (
    <section className="space-y-4 rounded-lg border border-slate-200 bg-white p-5">
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="flex items-center gap-2 text-lg font-semibold text-slate-900">
            <Users className="h-5 w-5 text-slate-500" />
            Моя пати
            <StatusBadge status={party.status} />
          </h2>
          <button
            type="button"
            onClick={() => void copyCode()}
            className="mt-1 inline-flex items-center gap-1.5 rounded bg-slate-50 px-2 py-1 font-mono text-xs text-slate-600 hover:bg-slate-100"
            title="Скопировать код"
          >
            {party.code}
            {copied ? (
              <Check className="h-3.5 w-3.5 text-emerald-600" />
            ) : (
              <Copy className="h-3.5 w-3.5" />
            )}
          </button>
        </div>
        <Button
          variant="ghost"
          onClick={() => actions.leave.mutate()}
          disabled={actions.isPending || party.status === "IN_MATCH"}
        >
          <LogOut className="mr-1 h-4 w-4" />
          Покинуть
        </Button>
      </header>

      <ul className="divide-y divide-slate-100 rounded-md border border-slate-200">
        {party.members.map((member) => {
          const isMe = member.accountId === accountId;
          const isPartyLeader = member.accountId === party.leaderAccountId;

          return (
            <li
              key={member.accountId}
              className="flex flex-wrap items-center justify-between gap-2 px-4 py-2.5"
            >
              <span className="flex items-center gap-2 text-sm">
                {isPartyLeader && <Crown className="h-4 w-4 text-amber-500" />}
                <span className="font-mono text-xs text-slate-700">
                  {shortId(member.accountId)}
                </span>
                {isMe && (
                  <span className="rounded bg-slate-100 px-1.5 py-0.5 text-[10px] font-medium text-slate-500">
                    вы
                  </span>
                )}
              </span>
              <span className="flex items-center gap-2">
                <ReadyBadge isReady={member.isReady} />
                {isLeader && !isMe && party.status === "FORMING" && (
                  <Button
                    variant="ghost"
                    onClick={() => actions.kick.mutate(member.accountId)}
                    disabled={actions.isPending}
                  >
                    Исключить
                  </Button>
                )}
              </span>
            </li>
          );
        })}
      </ul>

      {party.status !== "IN_MATCH" && (
        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant={me?.isReady ? "ghost" : "primary"}
            onClick={() => actions.setReady.mutate(!me?.isReady)}
            disabled={actions.isPending || party.status !== "FORMING"}
          >
            {me?.isReady ? "Снять готовность" : "Я готов"}
          </Button>

          {!isLeader && (
            <p className="text-xs text-slate-500">
              В очередь ставит лидер пати.
            </p>
          )}

          {isLeader && !inQueue && (
            <Button
              onClick={() => actions.enqueue.mutate(undefined)}
              disabled={actions.isPending || !canQueue}
            >
              <Search className="mr-1 h-4 w-4" />
              В очередь
            </Button>
          )}

          {isLeader && inQueue && (
            <Button
              variant="ghost"
              onClick={() => actions.leaveQueue.mutate()}
              disabled={actions.isPending}
            >
              Выйти из очереди
            </Button>
          )}

          {isLeader && !canQueue && !inQueue && (
            <p className="text-xs text-slate-500">
              Нужно 2–5 игроков и готовность всех участников.
            </p>
          )}
        </div>
      )}

      {state.ticket && (
        <p className="flex items-center gap-2 rounded-md bg-sky-50 px-3 py-2 text-sm text-sky-700">
          <Search className="h-4 w-4" />
          В очереди с {formatDate(state.ticket.enqueuedAt)} ({state.ticket.mode})
        </p>
      )}
    </section>
  );
}

function ActiveMatch() {
  const { data, isPending, error } = useQuery(myMatchQueryOptions());

  if (isPending) {
    return <Spinner label="Загружаем матч..." />;
  }

  if (error) {
    return <ApiErrorList error={error} fallback="Не удалось загрузить матч" />;
  }

  if (!data) {
    return null;
  }

  return <MatchCard match={data} />;
}

function MatchCard({ match }: { match: MatchDetails }) {
  return (
    <section className="space-y-4 rounded-lg border border-slate-200 bg-white p-5">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="flex items-center gap-2 text-lg font-semibold text-slate-900">
          <Swords className="h-5 w-5 text-slate-500" />
          Матч · {match.mode}
          <MatchStatusBadge status={match.status} />
        </h2>
        <p className="text-xs text-slate-500">
          {formatDate(match.startsAt)} — {formatDate(match.endsAt)}
        </p>
      </header>

      <div className="grid gap-3 sm:grid-cols-2">
        {match.parties.map((party) => {
          const isMine = party.partyId === match.myPartyId;
          const members = match.participants.filter(
            (participant) => participant.partyId === party.partyId,
          );
          const isWinner = match.winnerPartyId === party.partyId;

          return (
            <div
              key={party.partyId}
              className={`rounded-lg border p-4 ${
                isMine ? "border-slate-900 bg-slate-50" : "border-slate-200"
              }`}
            >
              <div className="flex items-center justify-between gap-2">
                <span className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
                  Сторона {party.side}
                  {isMine ? " · ваша" : ""}
                </span>
                {isWinner && <Trophy className="h-4 w-4 text-amber-500" />}
              </div>
              <p className="mt-1 text-2xl font-semibold text-slate-900">
                {party.score}
              </p>
              <ul className="mt-2 space-y-1">
                {members.map((member) => {
                  const result = match.results.find(
                    (entry) => entry.accountId === member.accountId,
                  );

                  return (
                    <li
                      key={member.accountId}
                      className="flex items-center justify-between gap-2 text-xs text-slate-600"
                    >
                      <span className="font-mono">
                        {shortId(member.accountId)}
                      </span>
                      <span>
                        {result
                          ? `${result.score} очк. · ${result.kills} убийств`
                          : "нет результата"}
                      </span>
                    </li>
                  );
                })}
              </ul>
            </div>
          );
        })}
      </div>

      {match.reward && (
        <div className="rounded-md bg-amber-50 px-3 py-2 text-sm text-amber-800">
          Награда за матч: {match.reward.participationAmount} +{" "}
          {match.reward.winBonusAmount} за победу{" "}
          {match.reward.currencyCode}
          {match.reward.items.length > 0
            ? ` и ${match.reward.items.length} предмет(ов)`
            : ""}
          .
        </div>
      )}

      {match.status === "ACTIVE" && match.results.length === 0 && (
        <p className="text-sm text-slate-500">
          Результаты появятся после ввода администратором.
        </p>
      )}
    </section>
  );
}

function LeaderboardPanel() {
  const { entries, isPending, error, me } = useLeaderboard(20);
  const accountId = useSessionStore(sessionSelectors.user)?.sub ?? "";

  return (
    <aside className="space-y-4">
      <section className="rounded-lg border border-slate-200 bg-white p-5">
        <h2 className="flex items-center gap-2 text-sm font-semibold text-slate-900">
          <Trophy className="h-4 w-4 text-slate-500" />
          Мой рейтинг
        </h2>
        {me ? (
          <dl className="mt-3 grid grid-cols-2 gap-2 text-sm">
            <Metric label="Место" value={me.rank ? `#${me.rank}` : "—"} />
            <Metric label="Очки" value={String(me.points)} />
            <Metric label="Победы" value={String(me.wins)} />
            <Metric label="Матчи" value={String(me.matchesPlayed)} />
          </dl>
        ) : (
          <p className="mt-2 text-sm text-slate-500">Пока нет матчей.</p>
        )}
      </section>

      <section className="rounded-lg border border-slate-200 bg-white p-5">
        <h2 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
          Топ игроков
        </h2>

        {isPending ? (
          <Spinner label="Загружаем рейтинг..." />
        ) : error ? (
          <ApiErrorList error={error} fallback="Рейтинг недоступен" />
        ) : entries.length === 0 ? (
          <p className="mt-2 text-sm text-slate-500">Рейтинг пока пуст.</p>
        ) : (
          <ol className="mt-3 divide-y divide-slate-100">
            {entries.map((entry) => (
              <LeaderboardRow
                key={entry.accountId}
                entry={entry}
                isMe={entry.accountId === accountId}
              />
            ))}
          </ol>
        )}
      </section>
    </aside>
  );
}

function LeaderboardRow({
  entry,
  isMe,
}: {
  entry: LeaderboardEntry;
  isMe: boolean;
}) {
  return (
    <li className="flex items-center justify-between gap-2 py-2 text-sm">
      <span className="flex min-w-0 items-center gap-2">
        <span className="w-6 shrink-0 text-right font-mono text-xs text-slate-400">
          {entry.rank}
        </span>
        <span
          className={`truncate font-mono text-xs ${
            isMe ? "font-semibold text-slate-900" : "text-slate-600"
          }`}
        >
          {shortId(entry.accountId)}
        </span>
      </span>
      <span className="shrink-0 text-xs text-slate-500">
        {entry.points} очк. · {entry.wins} поб.
      </span>
    </li>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-md bg-slate-50 px-3 py-2">
      <dt className="text-xs text-slate-500">{label}</dt>
      <dd className="font-medium text-slate-900">{value}</dd>
    </div>
  );
}

function ReadyBadge({ isReady }: { isReady: boolean }) {
  return (
    <span
      className={`rounded px-2 py-0.5 text-xs font-medium ${
        isReady
          ? "bg-emerald-50 text-emerald-700"
          : "bg-slate-100 text-slate-500"
      }`}
    >
      {isReady ? "готов" : "не готов"}
    </span>
  );
}

function StatusBadge({ status }: { status: Party["status"] }) {
  const label: Record<Party["status"], string> = {
    FORMING: "сбор",
    QUEUED: "в очереди",
    IN_MATCH: "в матче",
    CLOSED: "закрыта",
  };

  return (
    <span className="rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
      {label[status]}
    </span>
  );
}

function MatchStatusBadge({ status }: { status: MatchDetails["status"] }) {
  const className =
    status === "ACTIVE"
      ? "bg-emerald-50 text-emerald-700"
      : status === "FINISHED"
        ? "bg-sky-50 text-sky-700"
        : "bg-slate-100 text-slate-500";

  const label: Record<MatchDetails["status"], string> = {
    ACTIVE: "идёт",
    FINISHED: "завершён",
    CANCELLED: "отменён",
  };

  return (
    <span className={`rounded px-2 py-0.5 text-xs font-medium ${className}`}>
      {label[status]}
    </span>
  );
}

function shortId(value: string): string {
  return value.length > 8 ? `${value.slice(0, 8)}…` : value;
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
