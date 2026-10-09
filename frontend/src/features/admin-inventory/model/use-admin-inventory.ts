import { adminInventoryQueryOptions } from "@/src/entities/inventory";
import { useQuery } from "@tanstack/react-query";

export function useAdminInventory(accountId: string | null) {
  const { data, isPending, isFetching, error } = useQuery({
    ...adminInventoryQueryOptions(accountId ?? ""),
    enabled: accountId !== null && accountId.length > 0,
  });

  return {
    inventory: data ?? null,
    isPending,
    isFetching,
    error,
  };
}
