import { adminWalletQueryOptions } from "@/src/entities/wallet";
import { useQuery } from "@tanstack/react-query";

export function useAdminWallet(walletId: string) {
  const { data, isPending, isFetching, error } = useQuery(
    adminWalletQueryOptions(walletId),
  );

  return {
    wallet: data ?? null,
    isPending,
    isFetching,
    error,
  };
}
