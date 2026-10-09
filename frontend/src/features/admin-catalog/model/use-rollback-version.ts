import { catalogApi, catalogQueryKeys } from "@/src/entities/catalog";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export function useRollbackVersion() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (input: { targetNumber: number; note: string | null }) =>
      catalogApi.rollbackVersion(input.targetNumber, input.note),
    onSuccess: (version) => {
      void queryClient.invalidateQueries({ queryKey: catalogQueryKeys.all });
      toast.success(`Каталог откатан к версии v${version.number}`);
    },
  });

  return {
    rollbackVersion: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    reset: mutation.reset,
  };
}
