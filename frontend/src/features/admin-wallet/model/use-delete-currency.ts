import { walletApi, walletQueryKeys } from "@/src/entities/wallet";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export function useDeleteCurrency() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (code: string) => walletApi.deleteCurrency(code),
    onSuccess: (currency) => {
      void queryClient.invalidateQueries({
        queryKey: walletQueryKeys.currencies(),
      });
      toast.success(`Валюта ${currency.code} удалена`);
    },
  });

  return {
    deleteCurrency: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    variables: mutation.variables,
    reset: mutation.reset,
  };
}
