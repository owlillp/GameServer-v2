import { apiClient } from "@/src/shared/api/axios-instance";
import {
  assertEnvelopeSuccess,
  Envelope,
  unwrapEnvelope,
} from "@/src/shared/api/envelope";
import { queryOptions } from "@tanstack/react-query";
import type {
  LeaderboardEntry,
  MatchDetails,
  MyLeaderboard,
  Party,
  PartyState,
  QueueTicket,
} from "./types";

export const matchmakingApi = {
  state: async (signal?: AbortSignal): Promise<PartyState> => {
    const response = await apiClient.get<Envelope<PartyState>>(
      "/matchmaking/parties/me",
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  createParty: async (): Promise<Party> => {
    const response = await apiClient.post<Envelope<Party>>(
      "/matchmaking/parties",
    );

    return unwrapEnvelope(response.data);
  },

  joinParty: async (code: string): Promise<Party> => {
    const response = await apiClient.post<Envelope<Party>>(
      "/matchmaking/parties/join",
      { code },
    );

    return unwrapEnvelope(response.data);
  },

  leaveParty: async (): Promise<void> => {
    const response = await apiClient.post<Envelope<boolean>>(
      "/matchmaking/parties/leave",
    );

    assertEnvelopeSuccess(response.data);
  },

  setReady: async (isReady: boolean): Promise<Party> => {
    const response = await apiClient.put<Envelope<Party>>(
      "/matchmaking/parties/ready",
      { isReady },
    );

    return unwrapEnvelope(response.data);
  },

  kickMember: async (accountId: string): Promise<Party> => {
    const response = await apiClient.post<Envelope<Party>>(
      `/matchmaking/parties/kick/${accountId}`,
    );

    return unwrapEnvelope(response.data);
  },

  enqueue: async (mode?: string): Promise<QueueTicket> => {
    const response = await apiClient.post<Envelope<QueueTicket>>(
      "/matchmaking/queue",
      { mode: mode || null },
    );

    return unwrapEnvelope(response.data);
  },

  leaveQueue: async (): Promise<void> => {
    const response = await apiClient.delete<Envelope<boolean>>(
      "/matchmaking/queue",
    );

    assertEnvelopeSuccess(response.data);
  },

  myMatch: async (signal?: AbortSignal): Promise<MatchDetails | null> => {
    const response = await apiClient.get<Envelope<MatchDetails | null>>(
      "/matchmaking/matches/me",
      { signal },
    );

    assertEnvelopeSuccess(response.data);

    return response.data.result;
  },

  leaderboard: async (
    limit = 50,
    signal?: AbortSignal,
  ): Promise<LeaderboardEntry[]> => {
    const response = await apiClient.get<Envelope<LeaderboardEntry[]>>(
      "/matchmaking/leaderboard",
      { params: { limit }, signal },
    );

    return unwrapEnvelope(response.data);
  },

  myLeaderboard: async (signal?: AbortSignal): Promise<MyLeaderboard> => {
    const response = await apiClient.get<Envelope<MyLeaderboard>>(
      "/matchmaking/leaderboard/me",
      { signal },
    );

    return unwrapEnvelope(response.data);
  },
};

export const matchmakingQueryKeys = {
  all: ["matchmaking"] as const,
  state: () => [...matchmakingQueryKeys.all, "state"] as const,
  match: () => [...matchmakingQueryKeys.all, "match"] as const,
  leaderboard: (limit: number) =>
    [...matchmakingQueryKeys.all, "leaderboard", limit] as const,
  myLeaderboard: () => [...matchmakingQueryKeys.all, "me"] as const,
};

export const partyStateQueryOptions = () =>
  queryOptions({
    queryKey: matchmakingQueryKeys.state(),
    queryFn: ({ signal }) => matchmakingApi.state(signal),
    retry: false,
    staleTime: 5_000,
  });

export const myMatchQueryOptions = () =>
  queryOptions({
    queryKey: matchmakingQueryKeys.match(),
    queryFn: ({ signal }) => matchmakingApi.myMatch(signal),
    retry: false,
    staleTime: 10_000,
  });

export const leaderboardQueryOptions = (limit = 50) =>
  queryOptions({
    queryKey: matchmakingQueryKeys.leaderboard(limit),
    queryFn: ({ signal }) => matchmakingApi.leaderboard(limit, signal),
    retry: false,
    staleTime: 15_000,
  });

export const myLeaderboardQueryOptions = () =>
  queryOptions({
    queryKey: matchmakingQueryKeys.myLeaderboard(),
    queryFn: ({ signal }) => matchmakingApi.myLeaderboard(signal),
    retry: false,
    staleTime: 15_000,
  });
