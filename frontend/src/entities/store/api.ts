import { apiClient } from "@/src/shared/api/axios-instance";
import { Envelope, unwrapEnvelope } from "@/src/shared/api/envelope";
import { queryOptions } from "@tanstack/react-query";
import type {
  CreateOfferRequest,
  Offer,
  Order,
  PurchaseOfferRequest,
  UpdateOfferRequest,
} from "./types";

export const storeApi = {
  // GET /store/offers — активные офферы.
  offers: async (signal?: AbortSignal): Promise<Offer[]> => {
    const response = await apiClient.get<Envelope<Offer[]>>("/store/offers", {
      signal,
    });

    return unwrapEnvelope(response.data);
  },

  // GET /store/offers/{id} — карточка активного оффера.
  offer: async (offerId: string, signal?: AbortSignal): Promise<Offer> => {
    const response = await apiClient.get<Envelope<Offer>>(
      `/store/offers/${offerId}`,
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // POST /store/purchases — купить оффер (идемпотентно).
  purchase: async (request: PurchaseOfferRequest): Promise<Order> => {
    const response = await apiClient.post<Envelope<Order>>(
      "/store/purchases",
      request,
      {
        headers: { "Idempotency-Key": crypto.randomUUID() },
      },
    );

    return unwrapEnvelope(response.data);
  },

  // GET /store/orders — история покупок текущего игрока.
  myOrders: async (signal?: AbortSignal): Promise<Order[]> => {
    const response = await apiClient.get<Envelope<Order[]>>("/store/orders", {
      signal,
    });

    return unwrapEnvelope(response.data);
  },

  // GET /store/orders/{id} — статус покупки.
  order: async (orderId: string, signal?: AbortSignal): Promise<Order> => {
    const response = await apiClient.get<Envelope<Order>>(
      `/store/orders/${orderId}`,
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // GET /admin/store/offers — все офферы (store.view).
  adminOffers: async (signal?: AbortSignal): Promise<Offer[]> => {
    const response = await apiClient.get<Envelope<Offer[]>>(
      "/admin/store/offers",
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/store/offers — создать оффер (store.manage).
  createOffer: async (request: CreateOfferRequest): Promise<Offer> => {
    const response = await apiClient.post<Envelope<Offer>>(
      "/admin/store/offers",
      request,
    );

    return unwrapEnvelope(response.data);
  },

  // PUT /admin/store/offers/{id} — обновить оффер (store.manage).
  updateOffer: async (
    offerId: string,
    request: UpdateOfferRequest,
  ): Promise<Offer> => {
    const response = await apiClient.put<Envelope<Offer>>(
      `/admin/store/offers/${offerId}`,
      request,
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/store/offers/{id}/activate — активировать оффер.
  activateOffer: async (offerId: string): Promise<Offer> => {
    const response = await apiClient.post<Envelope<Offer>>(
      `/admin/store/offers/${offerId}/activate`,
    );

    return unwrapEnvelope(response.data);
  },

  // DELETE /admin/store/offers/{id} — архивировать оффер.
  archiveOffer: async (offerId: string): Promise<Offer> => {
    const response = await apiClient.delete<Envelope<Offer>>(
      `/admin/store/offers/${offerId}`,
    );

    return unwrapEnvelope(response.data);
  },
};

export const storeQueryKeys = {
  all: ["store"] as const,
  offers: () => [...storeQueryKeys.all, "offers"] as const,
  offer: (offerId: string) =>
    [...storeQueryKeys.all, "offers", offerId] as const,
  orders: () => [...storeQueryKeys.all, "orders"] as const,
  order: (orderId: string) =>
    [...storeQueryKeys.all, "orders", orderId] as const,
  adminOffers: () => [...storeQueryKeys.all, "admin", "offers"] as const,
};

export const offersQueryOptions = () =>
  queryOptions({
    queryKey: storeQueryKeys.offers(),
    queryFn: ({ signal }) => storeApi.offers(signal),
    retry: false,
    staleTime: 15_000,
  });

export const offerQueryOptions = (offerId: string) =>
  queryOptions({
    queryKey: storeQueryKeys.offer(offerId),
    queryFn: ({ signal }) => storeApi.offer(offerId, signal),
    retry: false,
    staleTime: 15_000,
  });

export const myOrdersQueryOptions = () =>
  queryOptions({
    queryKey: storeQueryKeys.orders(),
    queryFn: ({ signal }) => storeApi.myOrders(signal),
    retry: false,
    staleTime: 5_000,
  });

export const orderQueryOptions = (orderId: string) =>
  queryOptions({
    queryKey: storeQueryKeys.order(orderId),
    queryFn: ({ signal }) => storeApi.order(orderId, signal),
    retry: false,
    staleTime: 0,
  });

export const adminOffersQueryOptions = () =>
  queryOptions({
    queryKey: storeQueryKeys.adminOffers(),
    queryFn: ({ signal }) => storeApi.adminOffers(signal),
    retry: false,
    staleTime: 10_000,
  });
