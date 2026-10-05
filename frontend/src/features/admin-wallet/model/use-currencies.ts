import { currenciesQueryOptions } from "@/src/entities/wallet";
import { useQuery } from "@tanstack/react-query";

export function useCurrencies() {
  const { data, isPending, isFetching, error } = useQuery(
    currenciesQueryOptions(),
  );

  return {
    currencies: data ?? [],
    isPending,
    isFetching,
    error,
  };
}
