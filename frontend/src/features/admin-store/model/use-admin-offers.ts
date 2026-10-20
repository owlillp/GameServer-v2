import { adminOffersQueryOptions } from "@/src/entities/store";
import { useQuery } from "@tanstack/react-query";

export function useAdminOffers() {
  const { data, isPending, error } = useQuery(adminOffersQueryOptions());

  return {
    offers: data ?? [],
    isPending,
    error,
  };
}
