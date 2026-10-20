import { orderQueryOptions, type OrderStatus } from "@/src/entities/store";
import { useQuery } from "@tanstack/react-query";

const TERMINAL_STATUSES: OrderStatus[] = ["COMPLETED", "FAILED"];

export function useOrderStatus(orderId: string | null) {
  const { data, isPending, error } = useQuery({
    ...orderQueryOptions(orderId ?? ""),
    enabled: Boolean(orderId),
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status && TERMINAL_STATUSES.includes(status) ? false : 1500;
    },
  });

  return {
    order: data ?? null,
    isPending,
    error,
  };
}
