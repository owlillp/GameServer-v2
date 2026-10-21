import {
  leaderboardQueryOptions,
  myLeaderboardQueryOptions,
} from "@/src/entities/matchmaking";
import { useQuery } from "@tanstack/react-query";

export function useLeaderboard(limit = 20) {
  const leaderboard = useQuery(leaderboardQueryOptions(limit));
  const me = useQuery(myLeaderboardQueryOptions());

  return {
    entries: leaderboard.data ?? [],
    isPending: leaderboard.isPending,
    error: leaderboard.error,
    me: me.data ?? null,
    mePending: me.isPending,
    meError: me.error,
  };
}
