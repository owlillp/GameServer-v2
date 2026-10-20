import { storeApi, storeQueryKeys } from "@/src/entities/store";
import { useMutation, useQueryClient } from "@tanstack/react-query";

export function usePurchaseOffer() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (offerId: string) => storeApi.purchase({ offerId }),
    onSuccess: (order) => {
      queryClient.setQueryData(storeQueryKeys.order(order.id), order);
      void queryClient.invalidateQueries({ queryKey: storeQueryKeys.orders() });
    },
  });
}
