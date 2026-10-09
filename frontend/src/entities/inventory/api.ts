import { apiClient } from "@/src/shared/api/axios-instance";
import { Envelope, unwrapEnvelope } from "@/src/shared/api/envelope";
import { queryOptions } from "@tanstack/react-query";
import type {
  AddCarRequest,
  GrantItemsRequest,
  Inventory,
  RevokeItemsRequest,
} from "./types";

export const inventoryApi = {
  // GET /inventory/me — инвентарь текущего игрока.
  me: async (signal?: AbortSignal): Promise<Inventory> => {
    const response = await apiClient.get<Envelope<Inventory>>("/inventory/me", {
      signal,
    });

    return unwrapEnvelope(response.data);
  },

  // GET /admin/inventory/{accountId} — инвентарь игрока (inventory.view).
  adminInventory: async (
    accountId: string,
    signal?: AbortSignal,
  ): Promise<Inventory> => {
    const response = await apiClient.get<Envelope<Inventory>>(
      `/admin/inventory/${accountId}`,
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/inventory/{accountId}/grant (inventory.manage).
  adminGrant: async (
    accountId: string,
    request: GrantItemsRequest,
  ): Promise<Inventory> => {
    const response = await apiClient.post<Envelope<Inventory>>(
      `/admin/inventory/${accountId}/grant`,
      request,
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/inventory/{accountId}/revoke (inventory.manage).
  adminRevoke: async (
    accountId: string,
    request: RevokeItemsRequest,
  ): Promise<Inventory> => {
    const response = await apiClient.post<Envelope<Inventory>>(
      `/admin/inventory/${accountId}/revoke`,
      request,
    );

    return unwrapEnvelope(response.data);
  },

  // PUT /admin/inventory/{accountId}/outfit/{slot} (inventory.manage).
  adminEquip: async (
    accountId: string,
    slot: string,
    itemId: string,
  ): Promise<Inventory> => {
    const response = await apiClient.put<Envelope<Inventory>>(
      `/admin/inventory/${accountId}/outfit/${slot}`,
      { itemId },
    );

    return unwrapEnvelope(response.data);
  },

  // DELETE /admin/inventory/{accountId}/outfit/{slot} (inventory.manage).
  adminUnequip: async (
    accountId: string,
    slot: string,
  ): Promise<Inventory> => {
    const response = await apiClient.delete<Envelope<Inventory>>(
      `/admin/inventory/${accountId}/outfit/${slot}`,
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/inventory/{accountId}/cars (inventory.manage).
  adminAddCar: async (
    accountId: string,
    request: AddCarRequest,
  ): Promise<Inventory> => {
    const response = await apiClient.post<Envelope<Inventory>>(
      `/admin/inventory/${accountId}/cars`,
      request,
    );

    return unwrapEnvelope(response.data);
  },

  // DELETE /admin/inventory/{accountId}/cars/{carId} (inventory.manage).
  adminRemoveCar: async (
    accountId: string,
    carId: string,
  ): Promise<Inventory> => {
    const response = await apiClient.delete<Envelope<Inventory>>(
      `/admin/inventory/${accountId}/cars/${carId}`,
    );

    return unwrapEnvelope(response.data);
  },

  // PUT /admin/inventory/{accountId}/cars/{carId}/parts/{slot}.
  adminInstallPart: async (
    accountId: string,
    carId: string,
    slot: string,
    partItemId: string,
  ): Promise<Inventory> => {
    const response = await apiClient.put<Envelope<Inventory>>(
      `/admin/inventory/${accountId}/cars/${carId}/parts/${slot}`,
      { partItemId },
    );

    return unwrapEnvelope(response.data);
  },

  // DELETE /admin/inventory/{accountId}/cars/{carId}/parts/{slot}.
  adminRemovePart: async (
    accountId: string,
    carId: string,
    slot: string,
  ): Promise<Inventory> => {
    const response = await apiClient.delete<Envelope<Inventory>>(
      `/admin/inventory/${accountId}/cars/${carId}/parts/${slot}`,
    );

    return unwrapEnvelope(response.data);
  },
};

export const inventoryQueryKeys = {
  all: ["inventory"] as const,
  me: () => [...inventoryQueryKeys.all, "me"] as const,
  admin: (accountId: string) =>
    [...inventoryQueryKeys.all, "admin", accountId] as const,
};

export const myInventoryQueryOptions = () =>
  queryOptions({
    queryKey: inventoryQueryKeys.me(),
    queryFn: ({ signal }) => inventoryApi.me(signal),
    retry: false,
    staleTime: 15_000,
  });

export const adminInventoryQueryOptions = (accountId: string) =>
  queryOptions({
    queryKey: inventoryQueryKeys.admin(accountId),
    queryFn: ({ signal }) => inventoryApi.adminInventory(accountId, signal),
    retry: false,
    staleTime: 10_000,
  });
