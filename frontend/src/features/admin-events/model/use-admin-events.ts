import { adminEventsQueryOptions } from "@/src/entities/events";
import { useQuery } from "@tanstack/react-query";

export function useAdminEvents() {
  const { data, isPending, error } = useQuery(adminEventsQueryOptions());

  return {
    events: data ?? [],
    isPending,
    error,
  };
}
