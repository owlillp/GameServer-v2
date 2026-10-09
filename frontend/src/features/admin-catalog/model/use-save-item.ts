import {
  catalogApi,
  catalogQueryKeys,
  type CreateItemRequest,
  type ItemDetail,
  type UpdateItemRequest,
} from "@/src/entities/catalog";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export type SaveItemInput =
  | { mode: "update"; itemId: string; request: UpdateItemRequest }
  | { mode: "create"; request: CreateItemRequest };

export function useSaveItem() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (input: SaveItemInput): Promise<ItemDetail> =>
      input.mode === "update"
        ? catalogApi.updateItem(input.itemId, input.request)
        : catalogApi.createItem(input.request),
    onSuccess: (item, input) => {
      void queryClient.invalidateQueries({
        queryKey: catalogQueryKeys.all,
      });
      toast.success(
        input.mode === "update"
          ? `Предмет «${item.displayName}» обновлён`
          : `Предмет «${item.displayName}» создан`,
      );
    },
  });

  return {
    saveItem: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    reset: mutation.reset,
  };
}
