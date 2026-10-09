import { catalogApi, catalogQueryKeys } from "@/src/entities/catalog";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export function usePublishVersion() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (note: string | null) => catalogApi.publishVersion(note),
    onSuccess: (version) => {
      void queryClient.invalidateQueries({ queryKey: catalogQueryKeys.all });
      toast.success(
        `Опубликована версия v${version.number} · предметов: ${version.itemCount}`,
      );
    },
  });

  return {
    publishVersion: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    reset: mutation.reset,
  };
}
