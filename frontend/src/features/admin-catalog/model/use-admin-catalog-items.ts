import {
  adminCatalogItemsQueryOptions,
  type AdminItemsRequest,
} from "@/src/entities/catalog";
import { useQuery } from "@tanstack/react-query";

export function useAdminCatalogItems(request: AdminItemsRequest) {
  const { data, isPending, isFetching, error } = useQuery(
    adminCatalogItemsQueryOptions(request),
  );

  return {
    items: data ?? [],
    isPending,
    isFetching,
    error,
  };
}
