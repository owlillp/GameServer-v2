import {
  walletApi,
  walletQueryKeys,
  type WalletOperationRequest,
} from "@/src/entities/wallet";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

export type AdminWalletOperationType = "credit" | "debit";

type Variables = {
  type: AdminWalletOperationType;
  walletId: string;
  request: WalletOperationRequest;
};

export function useAdminWalletOperation() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: ({ type, walletId, request }: Variables) =>
      type === "credit"
        ? walletApi.adminCredit(walletId, request)
        : walletApi.adminDebit(walletId, request),
    onSuccess: (_wallet, variables) => {
      void queryClient.invalidateQueries({ queryKey: walletQueryKeys.all });
      toast.success(
        variables.type === "credit"
          ? "Средства начислены"
          : "Средства списаны",
      );
    },
  });

  return {
    run: mutation.mutateAsync,
    isPending: mutation.isPending,
    isError: mutation.isError,
    error: mutation.error,
    result: mutation.data ?? null,
    reset: mutation.reset,
  };
}
