import { apiClient } from "@/src/shared/api/axios-instance";
import { Envelope, unwrapEnvelope } from "@/src/shared/api/envelope";
import { queryOptions } from "@tanstack/react-query";
import type {
  CreateEventRequest,
  EventDetails,
  EventPhase,
  EventRewardClaim,
  GameEvent,
  UpdateEventRequest,
} from "./types";

export const eventsApi = {
  // GET /events — опубликованные ивенты.
  list: async (
    phase?: EventPhase,
    signal?: AbortSignal,
  ): Promise<GameEvent[]> => {
    const response = await apiClient.get<Envelope<GameEvent[]>>("/events", {
      params: { phase: phase || undefined },
      signal,
    });

    return unwrapEnvelope(response.data);
  },

  // GET /events/active — ивенты в фазе ACTIVE.
  active: async (signal?: AbortSignal): Promise<GameEvent[]> => {
    const response = await apiClient.get<Envelope<GameEvent[]>>(
      "/events/active",
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // GET /events/{code} — ивент, задания, награды и прогресс игрока.
  details: async (
    code: string,
    signal?: AbortSignal,
  ): Promise<EventDetails> => {
    const response = await apiClient.get<Envelope<EventDetails>>(
      `/events/${encodeURIComponent(code)}`,
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // POST /events/{code}/rewards/{rewardCode}/claim — обмен очков на награду.
  claimReward: async (
    code: string,
    rewardCode: string,
  ): Promise<EventRewardClaim> => {
    const response = await apiClient.post<Envelope<EventRewardClaim>>(
      `/events/${encodeURIComponent(code)}/rewards/${encodeURIComponent(rewardCode)}/claim`,
      undefined,
      { headers: { "Idempotency-Key": crypto.randomUUID() } },
    );

    return unwrapEnvelope(response.data);
  },

  // GET /events/me/claims — история обменов.
  myClaims: async (
    eventId?: string,
    signal?: AbortSignal,
  ): Promise<EventRewardClaim[]> => {
    const response = await apiClient.get<Envelope<EventRewardClaim[]>>(
      "/events/me/claims",
      { params: { eventId }, signal },
    );

    return unwrapEnvelope(response.data);
  },

  // GET /admin/events — все ивенты (events.view).
  adminList: async (signal?: AbortSignal): Promise<GameEvent[]> => {
    const response = await apiClient.get<Envelope<GameEvent[]>>(
      "/admin/events",
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/events — создать черновик (events.manage).
  create: async (request: CreateEventRequest): Promise<GameEvent> => {
    const response = await apiClient.post<Envelope<GameEvent>>(
      "/admin/events",
      request,
    );

    return unwrapEnvelope(response.data);
  },

  // PUT /admin/events/{id} — обновить черновик (events.manage).
  update: async (
    eventId: string,
    request: UpdateEventRequest,
  ): Promise<GameEvent> => {
    const response = await apiClient.put<Envelope<GameEvent>>(
      `/admin/events/${eventId}`,
      request,
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/events/{id}/publish — опубликовать (events.manage).
  publish: async (eventId: string): Promise<GameEvent> => {
    const response = await apiClient.post<Envelope<GameEvent>>(
      `/admin/events/${eventId}/publish`,
    );

    return unwrapEnvelope(response.data);
  },

  // DELETE /admin/events/{id} — архивировать (events.manage).
  archive: async (eventId: string): Promise<GameEvent> => {
    const response = await apiClient.delete<Envelope<GameEvent>>(
      `/admin/events/${eventId}`,
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/events/{id}/accounts/{accountId}/points — корректировка очков.
  adjustPoints: async (
    eventId: string,
    accountId: string,
    amount: number,
  ): Promise<unknown> => {
    const response = await apiClient.post<Envelope<unknown>>(
      `/admin/events/${eventId}/accounts/${accountId}/points`,
      { amount },
    );

    return unwrapEnvelope(response.data);
  },
};

export const eventsQueryKeys = {
  all: ["events"] as const,
  list: (phase?: EventPhase) =>
    [...eventsQueryKeys.all, "list", phase ?? "ALL"] as const,
  active: () => [...eventsQueryKeys.all, "active"] as const,
  details: (code: string) => [...eventsQueryKeys.all, "details", code] as const,
  claims: (eventId?: string) =>
    [...eventsQueryKeys.all, "claims", eventId ?? "ALL"] as const,
  adminList: () => [...eventsQueryKeys.all, "admin", "list"] as const,
};

export const eventsQueryOptions = (phase?: EventPhase) =>
  queryOptions({
    queryKey: eventsQueryKeys.list(phase),
    queryFn: ({ signal }) => eventsApi.list(phase, signal),
    retry: false,
    staleTime: 15_000,
  });

export const activeEventsQueryOptions = () =>
  queryOptions({
    queryKey: eventsQueryKeys.active(),
    queryFn: ({ signal }) => eventsApi.active(signal),
    retry: false,
    staleTime: 15_000,
  });

export const eventDetailsQueryOptions = (code: string) =>
  queryOptions({
    queryKey: eventsQueryKeys.details(code),
    queryFn: ({ signal }) => eventsApi.details(code, signal),
    retry: false,
    staleTime: 5_000,
  });

export const myEventClaimsQueryOptions = (eventId?: string) =>
  queryOptions({
    queryKey: eventsQueryKeys.claims(eventId),
    queryFn: ({ signal }) => eventsApi.myClaims(eventId, signal),
    retry: false,
    staleTime: 5_000,
  });

export const adminEventsQueryOptions = () =>
  queryOptions({
    queryKey: eventsQueryKeys.adminList(),
    queryFn: ({ signal }) => eventsApi.adminList(signal),
    retry: false,
    staleTime: 10_000,
  });
