import { offersQueryOptions } from "@/src/entities/store";
import { useQuery } from "@tanstack/react-query";

export function useStoreOffers() {
  const { data, isPending, error } = useQuery(offersQueryOptions());

  return {
    offers: data ?? [],
    isPending,
    error,
  };
}
