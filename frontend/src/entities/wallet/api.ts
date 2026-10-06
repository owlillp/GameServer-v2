import { apiClient } from "@/src/shared/api/axios-instance";
import { Envelope, unwrapEnvelope } from "@/src/shared/api/envelope";
import { queryOptions } from "@tanstack/react-query";
import type {
  CreateCurrencyRequest,
  Currency,
  LedgerEntry,
  Wallet,
  WalletOperationRequest,
} from "./types";

export const walletApi = {
  // GET /wallets/me — балансы текущего игрока.
  me: async (signal?: AbortSignal): Promise<Wallet> => {
    const response = await apiClient.get<Envelope<Wallet>>("/wallets/me", {
      signal,
    });

    return unwrapEnvelope(response.data);
  },

  // GET /wallets/me/transactions — история операций текущего игрока.
  transactions: async (
    limit: number,
    signal?: AbortSignal,
  ): Promise<LedgerEntry[]> => {
    const response = await apiClient.get<Envelope<LedgerEntry[]>>(
      "/wallets/me/transactions",
      { params: { limit }, signal },
    );

    return unwrapEnvelope(response.data);
  },

  // GET /wallets/currencies — динамический список доступных валют.
  currencies: async (signal?: AbortSignal): Promise<Currency[]> => {
    const response = await apiClient.get<Envelope<Currency[]>>(
      "/wallets/currencies",
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/wallets/{id}/credit — ручное начисление (wallets.manage).
  adminCredit: async (
    walletId: string,
    request: WalletOperationRequest,
  ): Promise<Wallet> => {
    const response = await apiClient.post<Envelope<Wallet>>(
      `/admin/wallets/${walletId}/credit`,
      request,
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/wallets/{id}/debit — ручное списание (wallets.manage).
  adminDebit: async (
    walletId: string,
    request: WalletOperationRequest,
  ): Promise<Wallet> => {
    const response = await apiClient.post<Envelope<Wallet>>(
      `/admin/wallets/${walletId}/debit`,
      request,
    );

    return unwrapEnvelope(response.data);
  },

  // POST /admin/wallets/currencies — создание валюты (wallets.manage).
  createCurrency: async (request: CreateCurrencyRequest): Promise<Currency> => {
    const response = await apiClient.post<Envelope<Currency>>(
      "/admin/wallets/currencies",
      request,
    );

    return unwrapEnvelope(response.data);
  },

  // DELETE /admin/wallets/currencies/{code} — деактивация валюты.
  deleteCurrency: async (code: string): Promise<Currency> => {
    const response = await apiClient.delete<Envelope<Currency>>(
      `/admin/wallets/currencies/${code}`,
    );

    return unwrapEnvelope(response.data);
  },

  // GET /admin/wallets/{id} — карточка кошелька игрока (wallets.view).
  adminWallet: async (
    walletId: string,
    signal?: AbortSignal,
  ): Promise<Wallet> => {
    const response = await apiClient.get<Envelope<Wallet>>(
      `/admin/wallets/${walletId}`,
      { signal },
    );

    return unwrapEnvelope(response.data);
  },

  // GET /admin/wallets/{id}/transactions — история операций игрока.
  adminTransactions: async (
    walletId: string,
    limit: number,
    signal?: AbortSignal,
  ): Promise<LedgerEntry[]> => {
    const response = await apiClient.get<Envelope<LedgerEntry[]>>(
      `/admin/wallets/${walletId}/transactions`,
      { params: { limit }, signal },
    );

    return unwrapEnvelope(response.data);
  },
};

export const walletQueryKeys = {
  all: ["wallet"] as const,
  me: () => [...walletQueryKeys.all, "me"] as const,
  transactions: (limit: number) =>
    [...walletQueryKeys.all, "transactions", limit] as const,
  currencies: () => [...walletQueryKeys.all, "currencies"] as const,
  adminWallet: (walletId: string) =>
    [...walletQueryKeys.all, "admin", walletId] as const,
  adminWalletTransactions: (walletId: string, limit: number) =>
    [...walletQueryKeys.all, "admin", walletId, "transactions", limit] as const,
};

export const walletQueryOptions = () =>
  queryOptions({
    queryKey: walletQueryKeys.me(),
    queryFn: ({ signal }) => walletApi.me(signal),
    retry: false,
    staleTime: 0,
  });

export const walletTransactionsQueryOptions = (limit = 10) =>
  queryOptions({
    queryKey: walletQueryKeys.transactions(limit),
    queryFn: ({ signal }) => walletApi.transactions(limit, signal),
    retry: false,
    staleTime: 0,
  });

export const currenciesQueryOptions = () =>
  queryOptions({
    queryKey: walletQueryKeys.currencies(),
    queryFn: ({ signal }) => walletApi.currencies(signal),
    staleTime: 60_000,
  });

export const adminWalletQueryOptions = (walletId: string) =>
  queryOptions({
    queryKey: walletQueryKeys.adminWallet(walletId),
    queryFn: ({ signal }) => walletApi.adminWallet(walletId, signal),
    retry: false,
    staleTime: 0,
  });

export const adminWalletTransactionsQueryOptions = (
  walletId: string,
  limit = 50,
) =>
  queryOptions({
    queryKey: walletQueryKeys.adminWalletTransactions(walletId, limit),
    queryFn: ({ signal }) => walletApi.adminTransactions(walletId, limit, signal),
    retry: false,
    staleTime: 0,
  });
