"use client";

import { adminCatalogItemsQueryOptions } from "@/src/entities/catalog";
import {
  EVENT_KINDS,
  QUEST_KINDS,
  QUEST_METRICS,
  type CreateEventRequest,
  type EventKind,
  type EventPhase,
  type EventQuestRequest,
  type EventRewardRequest,
  type GameEvent,
  type QuestKind,
  type QuestMetric,
  type UpdateEventRequest,
} from "@/src/entities/events";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Input } from "@/src/shared/ui/input";
import { Select } from "@/src/shared/ui/select";
import { Spinner } from "@/src/shared/ui/spinner";
import { Textarea } from "@/src/shared/ui/textarea";
import { useQuery } from "@tanstack/react-query";
import { Plus, Trash2 } from "lucide-react";
import { useState, type FormEvent } from "react";
import { useAdminEvents } from "../model/use-admin-events";
import {
  useAdjustEventPoints,
  useArchiveEvent,
  useCreateEvent,
  usePublishEvent,
  useUpdateEvent,
} from "../model/use-event-mutations";

export function AdminEventsManager() {
  const { events, isPending, error } = useAdminEvents();
  const [selected, setSelected] = useState<GameEvent | null>(null);

  if (isPending) {
    return <Spinner label="Загружаем ивенты..." />;
  }

  if (error) {
    return (
      <ApiErrorList error={error} fallback="Не удалось загрузить ивенты" />
    );
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.35fr)]">
      <section className="space-y-3">
        <div className="flex items-center justify-between gap-3">
          <h2 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
            Ивенты
          </h2>
          <Button variant="ghost" onClick={() => setSelected(null)}>
            <Plus className="mr-1 h-4 w-4" /> Новый
          </Button>
        </div>

        {events.length === 0 ? (
          <p className="rounded-md border border-dashed border-slate-300 px-4 py-6 text-center text-sm text-slate-500">
            Ивентов ещё нет.
          </p>
        ) : (
          <ul className="divide-y divide-slate-100 rounded-md border border-slate-200 bg-white">
            {events.map((event) => (
              <EventRow
                key={event.id}
                event={event}
                onSelect={() => setSelected(event)}
              />
            ))}
          </ul>
        )}
      </section>

      <EventForm
        key={selected?.id ?? "new"}
        event={selected}
        onDone={() => setSelected(null)}
      />
    </div>
  );
}

function EventRow({
  event,
  onSelect,
}: {
  event: GameEvent;
  onSelect: () => void;
}) {
  const publish = usePublishEvent();
  const archive = useArchiveEvent();

  return (
    <li className="flex flex-wrap items-center justify-between gap-2 px-4 py-2.5 text-sm">
      <button
        type="button"
        onClick={onSelect}
        className="text-left font-medium text-slate-800 hover:text-slate-950"
      >
        {event.displayName}
        <span className="ml-2 font-mono text-xs text-slate-400">
          {event.code}
        </span>
        <span className="ml-2 text-xs text-slate-500">
          {formatDate(event.startsAt)} — {formatDate(event.endsAt)}
        </span>
      </button>
      <span className="flex items-center gap-2">
        <PhaseBadge phase={event.phase} />
        {event.status === "DRAFT" && (
          <Button
            variant="ghost"
            onClick={() => publish.mutate(event.id)}
            disabled={publish.isPending}
          >
            Опубликовать
          </Button>
        )}
        {event.status !== "ARCHIVED" && (
          <Button
            variant="ghost"
            onClick={() => archive.mutate(event.id)}
            disabled={archive.isPending}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        )}
      </span>
    </li>
  );
}

function EventForm({
  event,
  onDone,
}: {
  event: GameEvent | null;
  onDone: () => void;
}) {
  const isCreate = event === null;
  const isDraft = event === null || event.status === "DRAFT";
  const catalog = useQuery(
    adminCatalogItemsQueryOptions({ status: "PUBLISHED" }),
  );
  const create = useCreateEvent();
  const update = useUpdateEvent();
  const adjust = useAdjustEventPoints();

  const [code, setCode] = useState(event?.code ?? "");
  const [displayName, setDisplayName] = useState(event?.displayName ?? "");
  const [description, setDescription] = useState(event?.description ?? "");
  const [kind, setKind] = useState<EventKind>(event?.kind ?? "SEASONAL");
  const [currencyCode, setCurrencyCode] = useState(
    event?.currencyCode ?? "EVENT",
  );
  const [announceAt, setAnnounceAt] = useState(
    toInputValue(event?.announceAt ?? null),
  );
  const [startsAt, setStartsAt] = useState(
    toInputValue(event?.startsAt ?? defaultStart()),
  );
  const [endsAt, setEndsAt] = useState(
    toInputValue(event?.endsAt ?? defaultEnd()),
  );
  const [graceEndsAt, setGraceEndsAt] = useState(
    toInputValue(event?.graceEndsAt ?? null),
  );
  const [quests, setQuests] = useState<EventQuestRequest[]>(
    (event?.quests ?? []).map((quest) => ({
      code: quest.code,
      displayName: quest.displayName,
      description: quest.description,
      kind: quest.kind,
      metric: quest.metric,
      target: quest.target,
      points: quest.points,
      sortOrder: quest.sortOrder,
    })),
  );
  const [rewards, setRewards] = useState<EventRewardRequest[]>(
    (event?.rewards ?? []).map((reward) => ({
      code: reward.code,
      displayName: reward.displayName,
      description: reward.description,
      pointsCost: reward.pointsCost,
      catalogItemId: reward.catalogItemId,
      itemCode: reward.itemCode,
      itemKind: reward.itemKind,
      slot: reward.slot,
      quantity: reward.quantity,
      sortOrder: reward.sortOrder,
      isRepeatable: reward.isRepeatable,
    })),
  );
  const [localError, setLocalError] = useState<string | null>(null);
  const [accountId, setAccountId] = useState("");
  const [amount, setAmount] = useState("10");

  const mutation = isCreate ? create : update;
  const error = mutation.error;
  const catalogItems = catalog.data ?? [];

  const updateQuest = (index: number, patch: Partial<EventQuestRequest>) => {
    setQuests((current) =>
      current.map((quest, currentIndex) =>
        currentIndex === index ? { ...quest, ...patch } : quest,
      ),
    );
  };

  const updateReward = (index: number, patch: Partial<EventRewardRequest>) => {
    setRewards((current) =>
      current.map((reward, currentIndex) =>
        currentIndex === index ? { ...reward, ...patch } : reward,
      ),
    );
  };

  const onSubmit = (formEvent: FormEvent) => {
    formEvent.preventDefault();
    setLocalError(null);

    const startsAtIso = toIsoOrNull(startsAt);
    const endsAtIso = toIsoOrNull(endsAt);

    if (!startsAtIso || !endsAtIso) {
      setLocalError("Укажите даты начала и окончания.");
      return;
    }

    const base: UpdateEventRequest = {
      displayName: displayName.trim(),
      description: description.trim() || null,
      kind,
      currencyCode: currencyCode.trim().toUpperCase(),
      announceAt: toIsoOrNull(announceAt),
      startsAt: startsAtIso,
      endsAt: endsAtIso,
      graceEndsAt: toIsoOrNull(graceEndsAt),
      quests,
      rewards,
    };

    if (isCreate) {
      const request: CreateEventRequest = { ...base, code: code.trim() };
      create.mutate(request, { onSuccess: onDone });
      return;
    }

    update.mutate({ eventId: event.id, request: base }, { onSuccess: onDone });
  };

  const onAdjust = (adjustEvent: FormEvent) => {
    adjustEvent.preventDefault();
    if (!event || !accountId.trim()) return;

    adjust.mutate({
      eventId: event.id,
      accountId: accountId.trim(),
      amount: Number(amount),
    });
  };

  return (
    <div className="space-y-4">
      <form
        onSubmit={onSubmit}
        className="space-y-4 rounded-lg border border-slate-200 bg-white p-4"
      >
        <div className="flex items-start justify-between gap-3">
          <div>
            <h2 className="text-sm font-semibold text-slate-900">
              {isCreate ? "Новый ивент" : `Ивент «${event.displayName}»`}
            </h2>
            {!isDraft && (
              <p className="mt-1 text-xs text-amber-700">
                Опубликованный ивент нельзя редактировать.
              </p>
            )}
          </div>
          {event && <PhaseBadge phase={event.phase} />}
        </div>

        {(error || localError) && (
          <ApiErrorList
            error={error ?? new Error(localError ?? "")}
            fallback="Не удалось сохранить ивент"
          />
        )}

        <fieldset
          disabled={!isDraft}
          className="grid gap-3 disabled:opacity-60 sm:grid-cols-2"
        >
          <Field label="Код">
            <Input
              value={code}
              onChange={(event) => setCode(event.target.value)}
              disabled={!isCreate}
              placeholder="summer-2026"
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
          <Field label="Тип">
            <Select
              value={kind}
              onChange={(event) => setKind(event.target.value as EventKind)}
            >
              {EVENT_KINDS.map((value) => (
                <option key={value} value={value}>
                  {value}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Валюта ивента">
            <Input
              value={currencyCode}
              onChange={(event) => setCurrencyCode(event.target.value)}
              required
            />
          </Field>
          <Field label="Анонс">
            <Input
              type="datetime-local"
              value={announceAt}
              onChange={(event) => setAnnounceAt(event.target.value)}
            />
          </Field>
          <Field label="Начало">
            <Input
              type="datetime-local"
              value={startsAt}
              onChange={(event) => setStartsAt(event.target.value)}
              required
            />
          </Field>
          <Field label="Окончание">
            <Input
              type="datetime-local"
              value={endsAt}
              onChange={(event) => setEndsAt(event.target.value)}
              required
            />
          </Field>
          <Field label="Конец окна наград">
            <Input
              type="datetime-local"
              value={graceEndsAt}
              onChange={(event) => setGraceEndsAt(event.target.value)}
            />
          </Field>
        </fieldset>

        <Field label="Описание">
          <Textarea
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            rows={2}
            disabled={!isDraft}
          />
        </Field>

        <fieldset disabled={!isDraft} className="space-y-2 disabled:opacity-60">
          <div className="flex items-center justify-between">
            <p className="text-xs font-medium text-slate-600">Задания</p>
            <Button
              type="button"
              variant="ghost"
              onClick={() =>
                setQuests((current) => [
                  ...current,
                  {
                    code: `quest-${current.length + 1}`,
                    displayName: "Новое задание",
                    description: null,
                    kind: "DAILY",
                    metric: "MATCHES",
                    target: 1,
                    points: 10,
                    sortOrder: current.length,
                  },
                ])
              }
            >
              <Plus className="h-4 w-4" />
            </Button>
          </div>

          {quests.length === 0 ? (
            <p className="text-sm text-slate-500">Заданий пока нет.</p>
          ) : (
            <ul className="space-y-2">
              {quests.map((quest, index) => (
                <li
                  key={index}
                  className="grid gap-2 rounded border border-slate-200 p-2 sm:grid-cols-2"
                >
                  <Field label="Код">
                    <Input
                      value={quest.code}
                      onChange={(event) =>
                        updateQuest(index, { code: event.target.value })
                      }
                      required
                    />
                  </Field>
                  <Field label="Название">
                    <Input
                      value={quest.displayName}
                      onChange={(event) =>
                        updateQuest(index, { displayName: event.target.value })
                      }
                      required
                    />
                  </Field>
                  <Field label="Период">
                    <Select
                      value={quest.kind}
                      onChange={(event) =>
                        updateQuest(index, {
                          kind: event.target.value as QuestKind,
                        })
                      }
                    >
                      {QUEST_KINDS.map((value) => (
                        <option key={value} value={value}>
                          {value}
                        </option>
                      ))}
                    </Select>
                  </Field>
                  <Field label="Метрика">
                    <Select
                      value={quest.metric}
                      onChange={(event) =>
                        updateQuest(index, {
                          metric: event.target.value as QuestMetric,
                        })
                      }
                    >
                      {QUEST_METRICS.map((value) => (
                        <option key={value} value={value}>
                          {value}
                        </option>
                      ))}
                    </Select>
                  </Field>
                  <div className="grid grid-cols-2 gap-2">
                    <Field label="Цель">
                      <Input
                        type="number"
                        min={1}
                        value={quest.target}
                        onChange={(event) =>
                          updateQuest(index, {
                            target: Math.max(1, Number(event.target.value)),
                          })
                        }
                      />
                    </Field>
                    <Field label="Очки">
                      <Input
                        type="number"
                        min={1}
                        value={quest.points}
                        onChange={(event) =>
                          updateQuest(index, {
                            points: Math.max(1, Number(event.target.value)),
                          })
                        }
                      />
                    </Field>
                  </div>
                  <div className="flex items-end">
                    <Button
                      type="button"
                      variant="ghost"
                      onClick={() =>
                        setQuests((current) =>
                          current.filter(
                            (_, currentIndex) => currentIndex !== index,
                          ),
                        )
                      }
                    >
                      <Trash2 className="mr-1 h-4 w-4" /> Удалить
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </fieldset>

        <fieldset disabled={!isDraft} className="space-y-2 disabled:opacity-60">
          <div className="flex items-center justify-between">
            <p className="text-xs font-medium text-slate-600">Награды</p>
            <Button
              type="button"
              variant="ghost"
              onClick={() =>
                setRewards((current) => [
                  ...current,
                  {
                    code: `reward-${current.length + 1}`,
                    displayName: "Новая награда",
                    description: null,
                    pointsCost: 100,
                    catalogItemId: "",
                    itemCode: "",
                    itemKind: "CLOTHING",
                    slot: null,
                    quantity: 1,
                    sortOrder: current.length,
                    isRepeatable: false,
                  },
                ])
              }
            >
              <Plus className="h-4 w-4" />
            </Button>
          </div>

          {rewards.length === 0 ? (
            <p className="text-sm text-slate-500">Наград пока нет.</p>
          ) : (
            <ul className="space-y-2">
              {rewards.map((reward, index) => {
                const currentItemExists = catalogItems.some(
                  (item) => item.id === reward.catalogItemId,
                );

                return (
                  <li
                    key={index}
                    className="grid gap-2 rounded border border-slate-200 p-2 sm:grid-cols-2"
                  >
                    <Field label="Код">
                      <Input
                        value={reward.code}
                        onChange={(event) =>
                          updateReward(index, { code: event.target.value })
                        }
                        required
                      />
                    </Field>
                    <Field label="Название">
                      <Input
                        value={reward.displayName}
                        onChange={(event) =>
                          updateReward(index, {
                            displayName: event.target.value,
                          })
                        }
                        required
                      />
                    </Field>
                    <Field label="Предмет каталога">
                      <Select
                        value={reward.catalogItemId}
                        onChange={(event) => {
                          const catalogItemId = event.target.value;
                          const item = catalogItems.find(
                            (entry) => entry.id === catalogItemId,
                          );

                          updateReward(index, {
                            catalogItemId,
                            itemCode: item?.code ?? "",
                            itemKind: item?.kind ?? "CLOTHING",
                            slot: item?.slot ?? null,
                          });
                        }}
                        required
                      >
                        <option value="">Выберите предмет</option>
                        {catalogItems.map((item) => (
                          <option key={item.id} value={item.id}>
                            {item.displayName} ({item.kind})
                          </option>
                        ))}
                        {reward.catalogItemId && !currentItemExists && (
                          <option value={reward.catalogItemId}>
                            {reward.itemCode || reward.catalogItemId}
                          </option>
                        )}
                      </Select>
                    </Field>
                    <div className="grid grid-cols-2 gap-2">
                      <Field label="Цена, очки">
                        <Input
                          type="number"
                          min={1}
                          value={reward.pointsCost}
                          onChange={(event) =>
                            updateReward(index, {
                              pointsCost: Math.max(
                                1,
                                Number(event.target.value),
                              ),
                            })
                          }
                        />
                      </Field>
                      <Field label="Количество">
                        <Input
                          type="number"
                          min={1}
                          value={reward.quantity}
                          onChange={(event) =>
                            updateReward(index, {
                              quantity: Math.max(1, Number(event.target.value)),
                            })
                          }
                        />
                      </Field>
                    </div>
                    <label className="flex items-center gap-2 text-sm text-slate-700">
                      <input
                        type="checkbox"
                        checked={reward.isRepeatable}
                        onChange={(event) =>
                          updateReward(index, {
                            isRepeatable: event.target.checked,
                          })
                        }
                      />
                      Можно получать повторно
                    </label>
                    <div className="flex items-end justify-between gap-2">
                      <span className="text-xs text-slate-500">
                        {reward.itemKind}
                        {reward.slot ? ` · ${reward.slot}` : ""}
                      </span>
                      <Button
                        type="button"
                        variant="ghost"
                        onClick={() =>
                          setRewards((current) =>
                            current.filter(
                              (_, currentIndex) => currentIndex !== index,
                            ),
                          )
                        }
                      >
                        <Trash2 className="mr-1 h-4 w-4" /> Удалить
                      </Button>
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </fieldset>

        <div className="flex justify-end gap-2">
          <Button type="button" variant="ghost" onClick={onDone}>
            Отмена
          </Button>
          <Button type="submit" disabled={mutation.isPending || !isDraft}>
            {mutation.isPending
              ? "Сохраняем..."
              : isCreate
                ? "Создать"
                : "Сохранить"}
          </Button>
        </div>
      </form>

      {event && (
        <form
          onSubmit={onAdjust}
          className="space-y-3 rounded-lg border border-slate-200 bg-white p-4"
        >
          <h2 className="text-sm font-semibold text-slate-900">
            Корректировка очков
          </h2>
          <p className="text-xs text-slate-500">
            Положительное число начисляет очки, отрицательное — списывает.
          </p>

          {adjust.error && (
            <ApiErrorList
              error={adjust.error}
              fallback="Не удалось скорректировать очки"
            />
          )}
          {adjust.isSuccess && (
            <p className="text-sm text-emerald-700">Очки обновлены.</p>
          )}

          <div className="grid gap-2 sm:grid-cols-[minmax(0,1fr)_7rem_auto]">
            <Field label="Account ID">
              <Input
                value={accountId}
                onChange={(event) => setAccountId(event.target.value)}
                placeholder="00000000-0000-0000-0000-000000000000"
                required
              />
            </Field>
            <Field label="Очки">
              <Input
                type="number"
                value={amount}
                onChange={(event) => setAmount(event.target.value)}
                required
              />
            </Field>
            <div className="flex items-end">
              <Button type="submit" disabled={adjust.isPending}>
                {adjust.isPending ? "..." : "Применить"}
              </Button>
            </div>
          </div>
        </form>
      )}
    </div>
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

const PHASE_LABEL: Record<EventPhase, string> = {
  DRAFT: "Черновик",
  SCHEDULED: "Запланирован",
  ANNOUNCED: "Анонс",
  ACTIVE: "Активен",
  GRACE: "Завершение",
  CLOSED: "Закрыт",
};

function defaultStart(): string {
  const date = new Date(Date.now() + 24 * 60 * 60 * 1000);
  return date.toISOString();
}

function defaultEnd(): string {
  const date = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000);
  return date.toISOString();
}

function toInputValue(iso: string | null): string {
  if (!iso) return "";

  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";

  const pad = (value: number) => String(value).padStart(2, "0");

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(
    date.getDate(),
  )}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function toIsoOrNull(value: string): string | null {
  if (!value) return null;

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
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
