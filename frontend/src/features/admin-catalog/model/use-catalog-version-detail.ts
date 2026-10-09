import { adminCatalogVersionDetailQueryOptions } from "@/src/entities/catalog";
import { useQuery } from "@tanstack/react-query";

export function useCatalogVersionDetail(
  number: number | null,
  enabled: boolean,
) {
  const { data, isPending, error } = useQuery({
    ...adminCatalogVersionDetailQueryOptions(number ?? 0),
    enabled: enabled && number !== null,
  });

  return {
    detail: data ?? null,
    isPending,
    error,
  };
}
