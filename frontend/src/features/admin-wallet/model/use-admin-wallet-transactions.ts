import { adminWalletTransactionsQueryOptions } from "@/src/entities/wallet";
import { useQuery } from "@tanstack/react-query";

export function useAdminWalletTransactions(walletId: string, limit = 50) {
  const { data, isPending, error } = useQuery(
    adminWalletTransactionsQueryOptions(walletId, limit),
  );

  return {
    transactions: data ?? [],
    isPending,
    error,
  };
}
