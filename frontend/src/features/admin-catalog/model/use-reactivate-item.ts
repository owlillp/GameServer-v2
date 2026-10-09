import { catalogApi, catalogQueryKeys } from "@/src/entities/catalog";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export function useReactivateItem() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (itemId: string) => catalogApi.reactivateItem(itemId),
    onSuccess: (item) => {
      void queryClient.invalidateQueries({ queryKey: catalogQueryKeys.all });
      toast.success(`Предмет «${item.displayName}» возвращён в работу`);
    },
  });

  return {
    reactivateItem: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    reset: mutation.reset,
  };
}
