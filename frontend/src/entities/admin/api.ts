import { apiClient } from "@/src/shared/api/axios-instance";
import { Envelope, unwrapEnvelope } from "@/src/shared/api/envelope";
import { keepPreviousData, queryOptions } from "@tanstack/react-query";
import type {
  AdminStats,
  AdminUser,
  AdminUsersRequest,
  PaginationResponse,
} from "./types";

export const adminApi = {
  // GET /admin/users — требует permission users.view (Admin/Moderator).
  getUsers: async (
    request: AdminUsersRequest,
    signal?: AbortSignal,
  ): Promise<PaginationResponse<AdminUser>> => {
    const response = await apiClient.get<
      Envelope<PaginationResponse<AdminUser>>
    >("/admin/users", {
      params: {
        page: request.page,
        pageSize: request.pageSize,
        search: request.search || undefined,
      },
      signal,
    });

    return unwrapEnvelope(response.data);
  },

  // GET /admin/stats — сводка по аккаунтам и ролям.
  getStats: async (signal?: AbortSignal): Promise<AdminStats> => {
    const response = await apiClient.get<Envelope<AdminStats>>("/admin/stats", {
      signal,
    });

    return unwrapEnvelope(response.data);
  },
};

export const adminQueryKeys = {
  all: ["admin"] as const,
  users: (request: AdminUsersRequest) =>
    [...adminQueryKeys.all, "users", request] as const,
  stats: () => [...adminQueryKeys.all, "stats"] as const,
};

export const adminUsersQueryOptions = (request: AdminUsersRequest) =>
  queryOptions({
    queryKey: adminQueryKeys.users(request),
    queryFn: ({ signal }) => adminApi.getUsers(request, signal),
    // При смене страницы/поиска не мигаем пустой таблицей.
    placeholderData: keepPreviousData,
    staleTime: 30_000,
  });

export const adminStatsQueryOptions = () =>
  queryOptions({
    queryKey: adminQueryKeys.stats(),
    queryFn: ({ signal }) => adminApi.getStats(signal),
    staleTime: 30_000,
  });
