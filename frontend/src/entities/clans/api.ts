import { apiClient } from "@/src/shared/api/axios-instance";
import { Envelope, unwrapEnvelope } from "@/src/shared/api/envelope";
import { queryOptions } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import type {
  Clan,
  ClanJoinRequest,
  ClanMember,
  CreateClanRequest,
  MyClan,
} from "./types";

export const clansApi = {
  // GET /clans/me — клан игрока; null, если игрок не в клане (404).
  mine: async (signal?: AbortSignal): Promise<MyClan | null> => {
    try {
      const response = await apiClient.get<Envelope<MyClan>>("/clans/me", {
        signal,
      });

      return unwrapEnvelope(response.data);
    } catch (error) {
      if (isAxiosError(error) && error.response?.status === 404) {
        return null;
      }

      throw error;
    }
  },

  // GET /clans?query= — поиск по префиксу тега или части имени.
  search: async (query: string, signal?: AbortSignal): Promise<Clan[]> => {
    const response = await apiClient.get<Envelope<Clan[]>>("/clans", {
      params: { query: query || undefined },
      signal,
    });

    return unwrapEnvelope(response.data);
  },

  // GET /clans/requests/me — мои приглашения и заявки.
  myRequests: async (signal?: AbortSignal): Promise<ClanJoinRequest[]> => {
    const response = await apiClient.get<Envelope<ClanJoinRequest[]>>(
      "/clans/requests/me",
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // GET /clans/{id}/requests — заявки в клан (офицер и лидер).
  clanRequests: async (
    clanId: string,
    signal?: AbortSignal,
  ): Promise<ClanJoinRequest[]> => {
    const response = await apiClient.get<Envelope<ClanJoinRequest[]>>(
      `/clans/${clanId}/requests`,
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  create: async (request: CreateClanRequest): Promise<MyClan> => {
    const response = await apiClient.post<Envelope<MyClan>>("/clans", request);
    return unwrapEnvelope(response.data);
  },

  requestJoin: async (clanId: string): Promise<ClanJoinRequest> => {
    const response = await apiClient.post<Envelope<ClanJoinRequest>>(
      `/clans/${clanId}/requests`,
    );
    return unwrapEnvelope(response.data);
  },

  invite: async (
    clanId: string,
    accountId: string,
  ): Promise<ClanJoinRequest> => {
    const response = await apiClient.post<Envelope<ClanJoinRequest>>(
      `/clans/${clanId}/invites`,
      { accountId },
    );
    return unwrapEnvelope(response.data);
  },

  accept: async (requestId: string): Promise<ClanMember> => {
    const response = await apiClient.post<Envelope<ClanMember>>(
      `/clans/requests/${requestId}/accept`,
    );
    return unwrapEnvelope(response.data);
  },

  decline: async (requestId: string): Promise<boolean> => {
    const response = await apiClient.post<Envelope<boolean>>(
      `/clans/requests/${requestId}/decline`,
    );
    return unwrapEnvelope(response.data);
  },

  changeRole: async (
    clanId: string,
    accountId: string,
    role: "OFFICER" | "MEMBER",
  ): Promise<ClanMember> => {
    const response = await apiClient.put<Envelope<ClanMember>>(
      `/clans/${clanId}/members/${accountId}/role`,
      { role },
    );
    return unwrapEnvelope(response.data);
  },

  kick: async (clanId: string, accountId: string): Promise<boolean> => {
    const response = await apiClient.delete<Envelope<boolean>>(
      `/clans/${clanId}/members/${accountId}`,
    );
    return unwrapEnvelope(response.data);
  },

  transferLeadership: async (
    clanId: string,
    accountId: string,
  ): Promise<Clan> => {
    const response = await apiClient.post<Envelope<Clan>>(
      `/clans/${clanId}/transfer-leadership`,
      { accountId },
    );
    return unwrapEnvelope(response.data);
  },

  leave: async (): Promise<boolean> => {
    const response = await apiClient.post<Envelope<boolean>>("/clans/me/leave");
    return unwrapEnvelope(response.data);
  },

  disband: async (clanId: string): Promise<boolean> => {
    const response = await apiClient.delete<Envelope<boolean>>(
      `/clans/${clanId}`,
    );
    return unwrapEnvelope(response.data);
  },
};

export const clansQueryKeys = {
  all: ["clans"] as const,
  mine: () => [...clansQueryKeys.all, "me"] as const,
  search: (query: string) => [...clansQueryKeys.all, "search", query] as const,
  myRequests: () => [...clansQueryKeys.all, "requests", "me"] as const,
  clanRequests: (clanId: string) =>
    [...clansQueryKeys.all, "requests", clanId] as const,
};

export const myClanQueryOptions = () =>
  queryOptions({
    queryKey: clansQueryKeys.mine(),
    queryFn: ({ signal }) => clansApi.mine(signal),
    retry: false,
    staleTime: 10_000,
  });

export const clanSearchQueryOptions = (query: string) =>
  queryOptions({
    queryKey: clansQueryKeys.search(query),
    queryFn: ({ signal }) => clansApi.search(query, signal),
    retry: false,
    staleTime: 10_000,
  });

export const myClanRequestsQueryOptions = () =>
  queryOptions({
    queryKey: clansQueryKeys.myRequests(),
    queryFn: ({ signal }) => clansApi.myRequests(signal),
    retry: false,
    staleTime: 10_000,
  });

export const clanRequestsQueryOptions = (clanId: string) =>
  queryOptions({
    queryKey: clansQueryKeys.clanRequests(clanId),
    queryFn: ({ signal }) => clansApi.clanRequests(clanId, signal),
    retry: false,
    staleTime: 10_000,
  });
