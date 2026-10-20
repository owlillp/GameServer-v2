import { eventsQueryOptions } from "@/src/entities/events";
import { useQuery } from "@tanstack/react-query";

export function useEvents() {
  const { data, isPending, error } = useQuery(eventsQueryOptions());

  return {
    events: data ?? [],
    isPending,
    error,
  };
}
