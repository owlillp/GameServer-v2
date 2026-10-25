import { clansQueryKeys } from "@/src/entities/clans";
import { useMutation, useQueryClient } from "@tanstack/react-query";

// Любое действие с кланом меняет состав, заявки или роль — обновляем все клановые запросы.
export function useClanAction<TArgs, TResult>(
  action: (args: TArgs) => Promise<TResult>,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: action,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: clansQueryKeys.all });
    },
  });
}
