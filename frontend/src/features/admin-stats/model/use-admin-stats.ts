import { adminStatsQueryOptions } from "@/src/entities/admin";
import { useQuery } from "@tanstack/react-query";

export function useAdminStats() {
  const { data, isPending, isFetching, error } = useQuery(
    adminStatsQueryOptions(),
  );

  return {
    stats: data ?? null,
    isPending,
    isFetching,
    error,
  };
}
