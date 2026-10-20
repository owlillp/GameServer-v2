import {
  adminOffersQueryOptions,
  offersQueryOptions,
  storeApi,
  type CreateOfferRequest,
  type UpdateOfferRequest,
} from "@/src/entities/store";
import { useMutation, useQueryClient } from "@tanstack/react-query";

function useInvalidateStore() {
  const queryClient = useQueryClient();

  return () => {
    void queryClient.invalidateQueries({
      queryKey: adminOffersQueryOptions().queryKey,
    });
    void queryClient.invalidateQueries({
      queryKey: offersQueryOptions().queryKey,
    });
  };
}

export function useCreateOffer() {
  const invalidate = useInvalidateStore();

  return useMutation({
    mutationFn: (request: CreateOfferRequest) => storeApi.createOffer(request),
    onSuccess: invalidate,
  });
}

export function useUpdateOffer() {
  const invalidate = useInvalidateStore();

  return useMutation({
    mutationFn: ({
      offerId,
      request,
    }: {
      offerId: string;
      request: UpdateOfferRequest;
    }) => storeApi.updateOffer(offerId, request),
    onSuccess: invalidate,
  });
}

export function useActivateOffer() {
  const invalidate = useInvalidateStore();

  return useMutation({
    mutationFn: (offerId: string) => storeApi.activateOffer(offerId),
    onSuccess: invalidate,
  });
}

export function useArchiveOffer() {
  const invalidate = useInvalidateStore();

  return useMutation({
    mutationFn: (offerId: string) => storeApi.archiveOffer(offerId),
    onSuccess: invalidate,
  });
}
