import { catalogApi, catalogQueryKeys } from "@/src/entities/catalog";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export function useArchiveItem() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (itemId: string) => catalogApi.archiveItem(itemId),
    onSuccess: (item) => {
      void queryClient.invalidateQueries({ queryKey: catalogQueryKeys.all });
      toast.success(`Предмет «${item.displayName}» архивирован`);
    },
  });

  return {
    archiveItem: mutation.mutateAsync,
    isPending: mutation.isPending,
    variables: mutation.variables,
    error: mutation.error,
    reset: mutation.reset,
  };
}
