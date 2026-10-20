import {
  eventDetailsQueryOptions,
  eventsApi,
  myEventClaimsQueryOptions,
} from "@/src/entities/events";
import { useMutation, useQueryClient } from "@tanstack/react-query";

export function useClaimReward() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventCode,
      rewardCode,
    }: {
      eventCode: string;
      rewardCode: string;
    }) => eventsApi.claimReward(eventCode, rewardCode),
    onSuccess: (_claim, variables) => {
      void queryClient.invalidateQueries({
        queryKey: eventDetailsQueryOptions(variables.eventCode).queryKey,
      });
      void queryClient.invalidateQueries({
        queryKey: myEventClaimsQueryOptions().queryKey,
      });
    },
  });
}
