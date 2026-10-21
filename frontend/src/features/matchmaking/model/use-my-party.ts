import { partyStateQueryOptions } from "@/src/entities/matchmaking";
import { useQuery } from "@tanstack/react-query";

export function useMyParty() {
  const { data, isPending, error } = useQuery(partyStateQueryOptions());

  return {
    state: data ?? null,
    isPending,
    error,
  };
}
