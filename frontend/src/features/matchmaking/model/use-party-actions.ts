import {
  matchmakingApi,
  matchmakingQueryKeys,
} from "@/src/entities/matchmaking";
import { useMutation, useQueryClient } from "@tanstack/react-query";

export function usePartyActions() {
  const queryClient = useQueryClient();

  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: matchmakingQueryKeys.state() }),
      queryClient.invalidateQueries({ queryKey: matchmakingQueryKeys.match() }),
      queryClient.invalidateQueries({
        queryKey: matchmakingQueryKeys.myLeaderboard(),
      }),
    ]);

  const create = useMutation({
    mutationFn: () => matchmakingApi.createParty(),
    onSuccess: () => void invalidate(),
  });

  const join = useMutation({
    mutationFn: (code: string) => matchmakingApi.joinParty(code),
    onSuccess: () => void invalidate(),
  });

  const leave = useMutation({
    mutationFn: () => matchmakingApi.leaveParty(),
    onSuccess: () => void invalidate(),
  });

  const setReady = useMutation({
    mutationFn: (isReady: boolean) => matchmakingApi.setReady(isReady),
    onSuccess: () => void invalidate(),
  });

  const kick = useMutation({
    mutationFn: (accountId: string) => matchmakingApi.kickMember(accountId),
    onSuccess: () => void invalidate(),
  });

  const enqueue = useMutation({
    mutationFn: (mode?: string) => matchmakingApi.enqueue(mode),
    onSuccess: () => void invalidate(),
  });

  const leaveQueue = useMutation({
    mutationFn: () => matchmakingApi.leaveQueue(),
    onSuccess: () => void invalidate(),
  });

  const isPending =
    create.isPending ||
    join.isPending ||
    leave.isPending ||
    setReady.isPending ||
    kick.isPending ||
    enqueue.isPending ||
    leaveQueue.isPending;

  return {
    create,
    join,
    leave,
    setReady,
    kick,
    enqueue,
    leaveQueue,
    isPending,
  };
}
