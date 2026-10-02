import {
  adminUsersQueryOptions,
  type AdminUsersRequest,
} from "@/src/entities/admin";
import { useQuery } from "@tanstack/react-query";

export function useAdminUsers(request: AdminUsersRequest) {
  const { data, isPending, isFetching, error } = useQuery(
    adminUsersQueryOptions(request),
  );

  return {
    users: data?.items ?? [],
    totalCount: data?.totalCount ?? 0,
    totalPages: data?.totalPages ?? 1,
    isPending,
    isFetching,
    error,
  };
}
