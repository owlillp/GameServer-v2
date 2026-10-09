import { apiClient } from "@/src/shared/api/axios-instance";
import { Envelope, unwrapEnvelope } from "@/src/shared/api/envelope";
import { keepPreviousData, queryOptions } from "@tanstack/react-query";
import type {
  AdminItemsRequest,
  CatalogVersion,
  CatalogVersionDetail,
  CreateItemRequest,
  ItemDetail,
  UpdateItemRequest,
} from "./types";

export const catalogApi = {
  // GET /admin/catalog/items — все предметы, включая черновики (catalog.view).
  adminItems: async (
    request: AdminItemsRequest,
    signal?: AbortSignal,
  ): Promise<ItemDetail[]> => {
    const response = await apiClient.get<Envelope<ItemDetail[]>>(
      "/admin/catalog/items",
      {
        params: {
          status: request.status || undefined,
          kind: request.kind || undefined,
        },
        signal,
      },
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/catalog/items — создать предмет (catalog.manage).
  createItem: async (request: CreateItemRequest): Promise<ItemDetail> => {
    const response = await apiClient.post<Envelope<ItemDetail>>(
      "/admin/catalog/items",
      request,
    );

    return unwrapEnvelope(response.data);
  },

  // PUT /admin/catalog/items/{id} — обновить предмет (catalog.manage).
  updateItem: async (
    itemId: string,
    request: UpdateItemRequest,
  ): Promise<ItemDetail> => {
    const response = await apiClient.put<Envelope<ItemDetail>>(
      `/admin/catalog/items/${itemId}`,
      request,
    );

    return unwrapEnvelope(response.data);
  },

  // DELETE /admin/catalog/items/{id} — архивировать предмет (catalog.manage).
  archiveItem: async (itemId: string): Promise<ItemDetail> => {
    const response = await apiClient.delete<Envelope<ItemDetail>>(
      `/admin/catalog/items/${itemId}`,
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/catalog/items/{id}/reactivate — вернуть предмет в работу.
  reactivateItem: async (itemId: string): Promise<ItemDetail> => {
    const response = await apiClient.post<Envelope<ItemDetail>>(
      `/admin/catalog/items/${itemId}/reactivate`,
    );

    return unwrapEnvelope(response.data);
  },

  // GET /admin/catalog/versions — список опубликованных версий (catalog.view).
  versions: async (signal?: AbortSignal): Promise<CatalogVersion[]> => {
    const response = await apiClient.get<Envelope<CatalogVersion[]>>(
      "/admin/catalog/versions",
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // GET /admin/catalog/versions/{number} — содержимое и статус версии.
  versionDetail: async (
    number: number,
    signal?: AbortSignal,
  ): Promise<CatalogVersionDetail> => {
    const response = await apiClient.get<Envelope<CatalogVersionDetail>>(
      `/admin/catalog/versions/${number}`,
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // GET /catalog/version — текущая опубликованная версия.
  currentVersion: async (signal?: AbortSignal): Promise<CatalogVersion | null> => {
    const response = await apiClient.get<Envelope<CatalogVersion>>(
      "/catalog/version",
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/catalog/versions — опубликовать новую версию (catalog.manage).
  publishVersion: async (note: string | null): Promise<CatalogVersion> => {
    const response = await apiClient.post<Envelope<CatalogVersion>>(
      "/admin/catalog/versions",
      { note },
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/catalog/versions/rollback — откат к версии (catalog.manage).
  rollbackVersion: async (
    targetNumber: number,
    note: string | null,
  ): Promise<CatalogVersion> => {
    const response = await apiClient.post<Envelope<CatalogVersion>>(
      "/admin/catalog/versions/rollback",
      { targetNumber, note },
    );

    return unwrapEnvelope(response.data);
  },
};

export const catalogQueryKeys = {
  all: ["catalog"] as const,
  adminItems: (request: AdminItemsRequest) =>
    [...catalogQueryKeys.all, "admin", "items", request] as const,
  versions: () => [...catalogQueryKeys.all, "admin", "versions"] as const,
  versionDetail: (number: number) =>
    [...catalogQueryKeys.all, "admin", "versions", number] as const,
  currentVersion: () =>
    [...catalogQueryKeys.all, "version", "current"] as const,
};

export const adminCatalogItemsQueryOptions = (request: AdminItemsRequest) =>
  queryOptions({
    queryKey: catalogQueryKeys.adminItems(request),
    queryFn: ({ signal }) => catalogApi.adminItems(request, signal),
    placeholderData: keepPreviousData,
    staleTime: 10_000,
  });

export const catalogCurrentVersionQueryOptions = () =>
  queryOptions({
    queryKey: catalogQueryKeys.currentVersion(),
    queryFn: ({ signal }) => catalogApi.currentVersion(signal),
    retry: false,
    staleTime: 15_000,
  });

export const adminCatalogVersionsQueryOptions = () =>
  queryOptions({
    queryKey: catalogQueryKeys.versions(),
    queryFn: ({ signal }) => catalogApi.versions(signal),
    retry: false,
    staleTime: 15_000,
  });

export const adminCatalogVersionDetailQueryOptions = (number: number) =>
  queryOptions({
    queryKey: catalogQueryKeys.versionDetail(number),
    queryFn: ({ signal }) => catalogApi.versionDetail(number, signal),
    retry: false,
    staleTime: 15_000,
  });
