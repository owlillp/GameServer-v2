import { myOrdersQueryOptions } from "@/src/entities/store";
import { useQuery } from "@tanstack/react-query";

export function useMyOrders() {
  const { data, isPending, error } = useQuery(myOrdersQueryOptions());

  return {
    orders: data ?? [],
    isPending,
    error,
  };
}
