"use client";

import {
  clanRequestsQueryOptions,
  clanSearchQueryOptions,
  clansApi,
  myClanQueryOptions,
  myClanRequestsQueryOptions,
  type ClanJoinRequest,
  type ClanMember,
  type MyClan,
} from "@/src/entities/clans";
import {
  sessionSelectors,
  useSessionStore,
} from "@/src/shared/stores/session-store";
import { ApiErrorList } from "@/src/shared/ui/api-error-list";
import { Button } from "@/src/shared/ui/button";
import { Input } from "@/src/shared/ui/input";
import { Spinner } from "@/src/shared/ui/spinner";
import { useQuery } from "@tanstack/react-query";
import { Crown, Shield, Trophy, Users } from "lucide-react";
import { useState } from "react";
import { useDebounce } from "use-debounce";
import { useClanAction } from "../model/use-clan-action";

const ROLE_LABELS = {
  LEADER: "Лидер",
  OFFICER: "Офицер",
  MEMBER: "Участник",
} as const;

export function ClansView() {
  const myClan = useQuery(myClanQueryOptions());

  if (myClan.isPending) {
    return <Spinner label="Загружаем клан..." />;
  }

  if (myClan.error) {
    return (
      <ApiErrorList error={myClan.error} fallback="Не удалось загрузить клан" />
    );
  }

  return myClan.data ? <MyClanPanel data={myClan.data} /> : <NoClanPanel />;
}

function MyClanPanel({ data }: { data: MyClan }) {
  const me = useSessionStore(sessionSelectors.user)?.sub ?? "";
  const { clan, myRole, members } = data;
  const canManage = myRole === "LEADER" || myRole === "OFFICER";
  const leave = useClanAction(() => clansApi.leave());
  const disband = useClanAction(() => clansApi.disband(clan.id));

  return (
    <div className="space-y-6">
      <section className="flex flex-wrap items-start justify-between gap-4 rounded-lg border border-slate-200 bg-white p-5">
        <div className="space-y-1">
          <p className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
            [{clan.tag}] · {ROLE_LABELS[myRole]}
          </p>
          <h2 className="text-xl font-semibold text-slate-900">{clan.name}</h2>
          {clan.description ? (
            <p className="text-sm text-slate-600">{clan.description}</p>
          ) : null}
          <div className="flex gap-4 pt-2 text-sm text-slate-600">
            <span className="inline-flex items-center gap-1">
              <Users className="h-4 w-4" /> {clan.memberCount} /{" "}
              {clan.maxMembers}
            </span>
            <span className="inline-flex items-center gap-1">
              <Trophy className="h-4 w-4" /> {clan.clanPoints} очков
            </span>
          </div>
        </div>
        <div className="flex gap-2">
          {myRole === "LEADER" ? (
            <Button
              variant="ghost"
              disabled={disband.isPending}
              onClick={() => disband.mutate(undefined)}
            >
              Распустить
            </Button>
          ) : null}
          <Button
            variant="ghost"
            disabled={leave.isPending}
            onClick={() => leave.mutate(undefined)}
          >
            Выйти
          </Button>
        </div>
      </section>

      {leave.error ? (
        <ApiErrorList
          error={leave.error}
          fallback="Не удалось выйти из клана"
        />
      ) : null}
      {disband.error ? (
        <ApiErrorList
          error={disband.error}
          fallback="Не удалось распустить клан"
        />
      ) : null}

      <MembersList clanId={clan.id} members={members} myRole={myRole} me={me} />

      {canManage ? <ClanRequestsPanel clanId={clan.id} /> : null}
    </div>
  );
}

function MembersList({
  clanId,
  members,
  myRole,
  me,
}: {
  clanId: string;
  members: ClanMember[];
  myRole: MyClan["myRole"];
  me: string;
}) {
  const changeRole = useClanAction(
    (args: { accountId: string; role: "OFFICER" | "MEMBER" }) =>
      clansApi.changeRole(clanId, args.accountId, args.role),
  );
  const kick = useClanAction((accountId: string) =>
    clansApi.kick(clanId, accountId),
  );
  const transfer = useClanAction((accountId: string) =>
    clansApi.transferLeadership(clanId, accountId),
  );
  const error = changeRole.error ?? kick.error ?? transfer.error;

  const rank = { LEADER: 3, OFFICER: 2, MEMBER: 1 } as const;

  return (
    <section className="space-y-2">
      <h3 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
        Состав
      </h3>
      {error ? (
        <ApiErrorList error={error} fallback="Действие не выполнено" />
      ) : null}
      <ul className="divide-y divide-slate-100 rounded-lg border border-slate-200 bg-white">
        {members.map((member) => {
          const isMe = member.accountId === me;
          const canKick = !isMe && rank[myRole] > rank[member.role];

          return (
            <li
              key={member.accountId}
              className="flex flex-wrap items-center gap-3 px-4 py-2 text-sm"
            >
              {member.role === "LEADER" ? (
                <Crown className="h-4 w-4 text-amber-500" />
              ) : member.role === "OFFICER" ? (
                <Shield className="h-4 w-4 text-slate-500" />
              ) : (
                <Users className="h-4 w-4 text-slate-300" />
              )}
              <span className="font-mono text-xs text-slate-700">
                {member.accountId}
                {isMe ? " (вы)" : ""}
              </span>
              <span className="text-slate-500">{ROLE_LABELS[member.role]}</span>
              <span className="ml-auto flex gap-1">
                {myRole === "LEADER" && !isMe ? (
                  <>
                    <Button
                      variant="ghost"
                      onClick={() =>
                        changeRole.mutate({
                          accountId: member.accountId,
                          role:
                            member.role === "OFFICER" ? "MEMBER" : "OFFICER",
                        })
                      }
                    >
                      {member.role === "OFFICER" ? "Понизить" : "Повысить"}
                    </Button>
                    <Button
                      variant="ghost"
                      onClick={() => transfer.mutate(member.accountId)}
                    >
                      Сделать лидером
                    </Button>
                  </>
                ) : null}
                {canKick ? (
                  <Button
                    variant="ghost"
                    onClick={() => kick.mutate(member.accountId)}
                  >
                    Исключить
                  </Button>
                ) : null}
              </span>
            </li>
          );
        })}
      </ul>
    </section>
  );
}

function ClanRequestsPanel({ clanId }: { clanId: string }) {
  const requests = useQuery(clanRequestsQueryOptions(clanId));
  const [accountId, setAccountId] = useState("");
  const invite = useClanAction((id: string) => clansApi.invite(clanId, id));

  return (
    <section className="space-y-2">
      <h3 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
        Заявки и приглашения
      </h3>
      <form
        className="flex gap-2"
        onSubmit={(event) => {
          event.preventDefault();
          invite.mutate(accountId.trim(), {
            onSuccess: () => setAccountId(""),
          });
        }}
      >
        <Input
          placeholder="ID игрока для приглашения"
          value={accountId}
          onChange={(event) => setAccountId(event.target.value)}
        />
        <Button type="submit" disabled={!accountId.trim() || invite.isPending}>
          Пригласить
        </Button>
      </form>
      {invite.error ? (
        <ApiErrorList error={invite.error} fallback="Не удалось пригласить" />
      ) : null}
      {requests.isPending ? (
        <Spinner label="Загружаем заявки..." />
      ) : (
        <RequestList
          requests={requests.data ?? []}
          empty="Нет ожидающих заявок"
          side="clan"
        />
      )}
    </section>
  );
}

function NoClanPanel() {
  const [query, setQuery] = useState("");
  const [debounced] = useDebounce(query, 300);
  const search = useQuery(clanSearchQueryOptions(debounced));
  const myRequests = useQuery(myClanRequestsQueryOptions());
  const requestJoin = useClanAction((clanId: string) =>
    clansApi.requestJoin(clanId),
  );

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,20rem)]">
      <section className="space-y-3">
        <h2 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
          Найти клан
        </h2>
        <Input
          placeholder="Тег или название"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />
        {requestJoin.error ? (
          <ApiErrorList
            error={requestJoin.error}
            fallback="Не удалось отправить заявку"
          />
        ) : null}
        {search.isPending ? (
          <Spinner label="Ищем..." />
        ) : (
          <ul className="divide-y divide-slate-100 rounded-lg border border-slate-200 bg-white">
            {(search.data ?? []).map((clan) => (
              <li
                key={clan.id}
                className="flex items-center gap-3 px-4 py-2 text-sm"
              >
                <span className="font-semibold text-slate-900">
                  [{clan.tag}]
                </span>
                <span className="text-slate-700">{clan.name}</span>
                <span className="text-slate-500">
                  {clan.memberCount}/{clan.maxMembers}
                </span>
                <Button
                  className="ml-auto"
                  variant="ghost"
                  disabled={
                    requestJoin.isPending || clan.memberCount >= clan.maxMembers
                  }
                  onClick={() => requestJoin.mutate(clan.id)}
                >
                  Подать заявку
                </Button>
              </li>
            ))}
          </ul>
        )}
      </section>

      <div className="space-y-6">
        <CreateClanForm />
        <section className="space-y-2">
          <h2 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
            Мои приглашения и заявки
          </h2>
          {myRequests.isPending ? (
            <Spinner label="Загружаем..." />
          ) : (
            <RequestList
              requests={myRequests.data ?? []}
              empty="Ничего нет"
              side="player"
            />
          )}
        </section>
      </div>
    </div>
  );
}

function CreateClanForm() {
  const [tag, setTag] = useState("");
  const [name, setName] = useState("");
  const create = useClanAction(() =>
    clansApi.create({ tag: tag.trim(), name: name.trim() }),
  );

  return (
    <form
      className="space-y-2 rounded-lg border border-slate-200 bg-white p-4"
      onSubmit={(event) => {
        event.preventDefault();
        create.mutate(undefined);
      }}
    >
      <h2 className="text-sm font-semibold text-slate-900">Создать клан</h2>
      <Input
        placeholder="Тег (3–6 символов)"
        maxLength={6}
        value={tag}
        onChange={(event) => setTag(event.target.value)}
      />
      <Input
        placeholder="Название"
        maxLength={32}
        value={name}
        onChange={(event) => setName(event.target.value)}
      />
      {create.error ? (
        <ApiErrorList error={create.error} fallback="Не удалось создать клан" />
      ) : null}
      <Button
        type="submit"
        className="w-full"
        disabled={
          create.isPending || tag.trim().length < 3 || name.trim().length < 3
        }
      >
        Создать
      </Button>
    </form>
  );
}

function RequestList({
  requests,
  empty,
  side,
}: {
  requests: ClanJoinRequest[];
  empty: string;
  // Решает приглашённый игрок (INVITE) или клан (REQUEST); другая сторона может только ждать или отозвать свою заявку.
  side: "player" | "clan";
}) {
  const accept = useClanAction((id: string) => clansApi.accept(id));
  const decline = useClanAction((id: string) => clansApi.decline(id));
  const error = accept.error ?? decline.error;

  if (requests.length === 0) {
    return <p className="text-sm text-slate-500">{empty}</p>;
  }

  return (
    <>
      {error ? (
        <ApiErrorList error={error} fallback="Действие не выполнено" />
      ) : null}
      <ul className="divide-y divide-slate-100 rounded-lg border border-slate-200 bg-white">
        {requests.map((request) => (
          <li
            key={request.id}
            className="flex flex-wrap items-center gap-2 px-4 py-2 text-sm"
          >
            <span className="font-semibold text-slate-900">
              [{request.clanTag}]
            </span>
            <span className="text-slate-500">
              {request.kind === "INVITE" ? "приглашение" : "заявка"}
            </span>
            <span className="font-mono text-xs text-slate-600">
              {request.accountId}
            </span>
            <RequestActions
              request={request}
              side={side}
              onAccept={() => accept.mutate(request.id)}
              onDecline={() => decline.mutate(request.id)}
            />
          </li>
        ))}
      </ul>
    </>
  );
}

function RequestActions({
  request,
  side,
  onAccept,
  onDecline,
}: {
  request: ClanJoinRequest;
  side: "player" | "clan";
  onAccept: () => void;
  onDecline: () => void;
}) {
  const decidedByMe =
    (side === "player" && request.kind === "INVITE") ||
    (side === "clan" && request.kind === "REQUEST");

  if (decidedByMe) {
    return (
      <span className="ml-auto flex gap-1">
        <Button variant="ghost" onClick={onAccept}>
          Принять
        </Button>
        <Button variant="ghost" onClick={onDecline}>
          Отклонить
        </Button>
      </span>
    );
  }

  return side === "player" ? (
    <Button className="ml-auto" variant="ghost" onClick={onDecline}>
      Отозвать
    </Button>
  ) : (
    <span className="ml-auto text-slate-400">ждёт ответа игрока</span>
  );
}
