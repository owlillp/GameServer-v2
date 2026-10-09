import { catalogCurrentVersionQueryOptions } from "@/src/entities/catalog";
import { useQuery } from "@tanstack/react-query";

export function useCurrentVersion() {
  const { data, isPending, error } = useQuery(
    catalogCurrentVersionQueryOptions(),
  );

  return {
    version: data ?? null,
    isPending,
    error,
  };
}
