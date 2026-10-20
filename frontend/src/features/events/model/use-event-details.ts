import { eventDetailsQueryOptions } from "@/src/entities/events";
import { useQuery } from "@tanstack/react-query";

export function useEventDetails(code: string | null) {
  const { data, isPending, error } = useQuery({
    ...eventDetailsQueryOptions(code ?? ""),
    enabled: Boolean(code),
  });

  return {
    details: data ?? null,
    isPending,
    error,
  };
}
