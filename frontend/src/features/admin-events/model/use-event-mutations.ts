import {
  adminEventsQueryOptions,
  eventsApi,
  eventsQueryKeys,
  type CreateEventRequest,
  type UpdateEventRequest,
} from "@/src/entities/events";
import { useMutation, useQueryClient } from "@tanstack/react-query";

function useInvalidateEvents() {
  const queryClient = useQueryClient();

  return () => {
    void queryClient.invalidateQueries({
      queryKey: adminEventsQueryOptions().queryKey,
    });
    void queryClient.invalidateQueries({
      queryKey: eventsQueryKeys.all,
    });
  };
}

export function useCreateEvent() {
  const invalidate = useInvalidateEvents();

  return useMutation({
    mutationFn: (request: CreateEventRequest) => eventsApi.create(request),
    onSuccess: invalidate,
  });
}

export function useUpdateEvent() {
  const invalidate = useInvalidateEvents();

  return useMutation({
    mutationFn: ({
      eventId,
      request,
    }: {
      eventId: string;
      request: UpdateEventRequest;
    }) => eventsApi.update(eventId, request),
    onSuccess: invalidate,
  });
}

export function usePublishEvent() {
  const invalidate = useInvalidateEvents();

  return useMutation({
    mutationFn: (eventId: string) => eventsApi.publish(eventId),
    onSuccess: invalidate,
  });
}

export function useArchiveEvent() {
  const invalidate = useInvalidateEvents();

  return useMutation({
    mutationFn: (eventId: string) => eventsApi.archive(eventId),
    onSuccess: invalidate,
  });
}

export function useAdjustEventPoints() {
  const invalidate = useInvalidateEvents();

  return useMutation({
    mutationFn: ({
      eventId,
      accountId,
      amount,
    }: {
      eventId: string;
      accountId: string;
      amount: number;
    }) => eventsApi.adjustPoints(eventId, accountId, amount),
    onSuccess: invalidate,
  });
}
