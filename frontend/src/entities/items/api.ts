import { apiClient } from "@/src/shared/api/axios-instance";
import {
  assertEnvelopeSuccess,
  Envelope,
  unwrapEnvelope,
} from "@/src/shared/api/envelope";
import { keepPreviousData, queryOptions } from "@tanstack/react-query";
import type {
  CreateItemRequest,
  Item,
  ItemsPage,
  ItemsRequest,
  UpdateItemRequest,
} from "./types";

// Все методы items требуют access-токен AuthService (Bearer ставит интерсептор).
export const itemsApi = {
  getItems: async (
    request: ItemsRequest,
    signal?: AbortSignal,
  ): Promise<ItemsPage> => {
    const response = await apiClient.get<Envelope<ItemsPage>>("/items", {
      params: {
        page: request.page,
        pageSize: request.pageSize,
        search: request.search || undefined,
      },
      signal,
    });

    return unwrapEnvelope(response.data);
  },

  getItem: async (itemId: string, signal?: AbortSignal): Promise<Item> => {
    const response = await apiClient.get<Envelope<Item>>(`/items/${itemId}`, {
      signal,
    });

    return unwrapEnvelope(response.data);
  },

  createItem: async (request: CreateItemRequest): Promise<Item> => {
    const response = await apiClient.post<Envelope<Item>>("/items", request);

    return unwrapEnvelope(response.data);
  },

  updateItem: async (
    itemId: string,
    request: UpdateItemRequest,
  ): Promise<Item> => {
    const response = await apiClient.put<Envelope<Item>>(
      `/items/${itemId}`,
      request,
    );

    return unwrapEnvelope(response.data);
  },

  deleteItem: async (itemId: string): Promise<void> => {
    const response = await apiClient.delete<Envelope>(`/items/${itemId}`);
    assertEnvelopeSuccess(response.data);
  },
};

export const itemsQueryKeys = {
  all: ["items"] as const,
  list: (request: ItemsRequest) =>
    [...itemsQueryKeys.all, "list", request] as const,
  details: (itemId: string) =>
    [...itemsQueryKeys.all, "details", itemId] as const,
};

export const itemsListQueryOptions = (request: ItemsRequest) =>
  queryOptions({
    queryKey: itemsQueryKeys.list(request),
    queryFn: ({ signal }) => itemsApi.getItems(request, signal),
    placeholderData: keepPreviousData,
    staleTime: 10_000,
  });

export const itemDetailsQueryOptions = (itemId: string) =>
  queryOptions({
    queryKey: itemsQueryKeys.details(itemId),
    queryFn: ({ signal }) => itemsApi.getItem(itemId, signal),
    staleTime: 5_000,
  });
