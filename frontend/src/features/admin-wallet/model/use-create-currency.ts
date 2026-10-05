import {
  walletApi,
  walletQueryKeys,
  type CreateCurrencyRequest,
} from "@/src/entities/wallet";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export function useCreateCurrency() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (request: CreateCurrencyRequest) =>
      walletApi.createCurrency(request),
    onSuccess: (currency) => {
      void queryClient.invalidateQueries({
        queryKey: walletQueryKeys.currencies(),
      });
      toast.success(`Валюта ${currency.code} создана`);
    },
  });

  return {
    createCurrency: mutation.mutateAsync,
    isPending: mutation.isPending,
    isError: mutation.isError,
    error: mutation.error,
    reset: mutation.reset,
  };
}
