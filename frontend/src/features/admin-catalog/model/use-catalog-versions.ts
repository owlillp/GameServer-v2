import { adminCatalogVersionsQueryOptions } from "@/src/entities/catalog";
import { useQuery } from "@tanstack/react-query";

export function useCatalogVersions() {
  const { data, isPending, error } = useQuery(
    adminCatalogVersionsQueryOptions(),
  );

  return {
    versions: data ?? [],
    isPending,
    error,
  };
}
