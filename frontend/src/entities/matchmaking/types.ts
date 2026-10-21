export const PARTY_STATUSES = [
  "FORMING",
  "QUEUED",
  "IN_MATCH",
  "CLOSED",
] as const;
export type PartyStatus = (typeof PARTY_STATUSES)[number];

export const MATCH_STATUSES = ["ACTIVE", "FINISHED", "CANCELLED"] as const;
export type MatchStatus = (typeof MATCH_STATUSES)[number];

export const QUEUE_STATUSES = ["QUEUED", "MATCHED", "CANCELLED"] as const;
export type QueueStatus = (typeof QUEUE_STATUSES)[number];

export type PartyMember = {
  accountId: string;
  isReady: boolean;
  joinedAt: string;
};

export type Party = {
  id: string;
  code: string;
  leaderAccountId: string;
  status: PartyStatus;
  size: number;
  createdAt: string;
  members: PartyMember[];
};

export type QueueTicket = {
  id: string;
  partyId: string;
  mode: string;
  status: QueueStatus;
  partySize: number;
  enqueuedAt: string;
  matchId: string | null;
};

export type MatchSummary = {
  id: string;
  mode: string;
  status: MatchStatus;
  startsAt: string;
  endsAt: string;
  finishedAt: string | null;
  winnerPartyId: string | null;
};

export type PartyState = {
  party: Party | null;
  ticket: QueueTicket | null;
  activeMatch: MatchSummary | null;
};

export type MatchParty = {
  partyId: string;
  side: number;
  score: number;
};

export type MatchParticipant = {
  accountId: string;
  partyId: string;
};

export type MatchResult = {
  accountId: string;
  partyId: string;
  score: number;
  kills: number;
  isWin: boolean;
  updatedAt: string;
};

export type MatchRewardItem = {
  itemId: string;
  code: string;
  kind: string;
  slot: string | null;
  quantity: number;
};

export type MatchReward = {
  currencyCode: string;
  participationAmount: number;
  winBonusAmount: number;
  items: MatchRewardItem[];
};

export type MatchDetails = {
  id: string;
  mode: string;
  status: MatchStatus;
  startsAt: string;
  endsAt: string;
  finishedAt: string | null;
  winnerPartyId: string | null;
  myPartyId: string | null;
  parties: MatchParty[];
  participants: MatchParticipant[];
  results: MatchResult[];
  reward: MatchReward | null;
};

export type LeaderboardEntry = {
  rank: number;
  accountId: string;
  points: number;
  wins: number;
  matchesPlayed: number;
};

export type MyLeaderboard = {
  rank: number | null;
  points: number;
  wins: number;
  matchesPlayed: number;
};
