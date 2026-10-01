import {
  itemDetailsQueryOptions,
  itemsApi,
  itemsListQueryOptions,
  itemsQueryKeys,
  type ItemsRequest,
} from "@/src/entities/items";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export function useItemsList(request: ItemsRequest) {
  const { data, isPending, isFetching, error } = useQuery(
    itemsListQueryOptions(request),
  );

  return {
    items: data?.items ?? [],
    totalCount: data?.totalCount ?? 0,
    totalPages: data?.totalPages ?? 1,
    isPending,
    isFetching,
    error,
  };
}

export function useItemDetails(itemId: string) {
  const { data, isPending, isFetching, error } = useQuery(
    itemDetailsQueryOptions(itemId),
  );

  return {
    item: data ?? null,
    isPending,
    isFetching,
    error,
  };
}

function useInvalidateItems() {
  const queryClient = useQueryClient();

  return () => queryClient.invalidateQueries({ queryKey: itemsQueryKeys.all });
}

export function useCreateItem() {
  const invalidate = useInvalidateItems();

  const mutation = useMutation({
    mutationFn: itemsApi.createItem,
    onSuccess: () => {
      void invalidate();
      toast.success("Запись создана");
    },
  });

  return {
    createItem: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
  };
}

export function useUpdateItem() {
  const invalidate = useInvalidateItems();

  const mutation = useMutation({
    mutationFn: ({
      itemId,
      request,
    }: {
      itemId: string;
      request: { name: string; description?: string | null };
    }) => itemsApi.updateItem(itemId, request),
    onSuccess: () => {
      void invalidate();
      toast.success("Запись обновлена");
    },
  });

  return {
    updateItem: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
  };
}

export function useDeleteItem() {
  const invalidate = useInvalidateItems();

  const mutation = useMutation({
    mutationFn: (itemId: string) => itemsApi.deleteItem(itemId),
    onSuccess: () => {
      void invalidate();
      toast.success("Запись удалена");
    },
  });

  return {
    deleteItem: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
  };
}
