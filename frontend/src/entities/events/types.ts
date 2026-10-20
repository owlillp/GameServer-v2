// Зеркало EventService.Contracts (camelCase).

export const EVENT_KINDS = ["SEASONAL", "HOLIDAY", "SPECIAL"] as const;
export type EventKind = (typeof EVENT_KINDS)[number];

export const EVENT_STATUSES = ["DRAFT", "PUBLISHED", "ARCHIVED"] as const;
export type EventStatus = (typeof EVENT_STATUSES)[number];

export const EVENT_PHASES = [
  "DRAFT",
  "SCHEDULED",
  "ANNOUNCED",
  "ACTIVE",
  "GRACE",
  "CLOSED",
] as const;
export type EventPhase = (typeof EVENT_PHASES)[number];

export const QUEST_KINDS = ["DAILY", "WEEKLY", "SEASONAL"] as const;
export type QuestKind = (typeof QUEST_KINDS)[number];

export const QUEST_METRICS = ["MATCHES", "WINS", "KILLS", "SCORE"] as const;
export type QuestMetric = (typeof QUEST_METRICS)[number];

export const CLAIM_STATUSES = ["PENDING", "COMPLETED", "FAILED"] as const;
export type ClaimStatus = (typeof CLAIM_STATUSES)[number];

export type EventQuest = {
  id: string;
  code: string;
  displayName: string;
  description: string | null;
  kind: QuestKind;
  metric: QuestMetric;
  target: number;
  points: number;
  sortOrder: number;
};

export type EventReward = {
  id: string;
  code: string;
  displayName: string;
  description: string | null;
  pointsCost: number;
  catalogItemId: string;
  itemCode: string;
  itemKind: string;
  slot: string | null;
  quantity: number;
  sortOrder: number;
  isRepeatable: boolean;
};

export type GameEvent = {
  id: string;
  code: string;
  displayName: string;
  description: string | null;
  kind: EventKind;
  status: EventStatus;
  phase: EventPhase;
  currencyCode: string;
  announceAt: string;
  startsAt: string;
  endsAt: string;
  graceEndsAt: string;
  quests: EventQuest[];
  rewards: EventReward[];
  createdAt: string;
  updatedAt: string;
};

export type EventQuestProgress = {
  questId: string;
  questCode: string;
  progress: number;
  target: number;
  isCompleted: boolean;
  periodStart: string;
};

export type EventProgress = {
  eventId: string;
  accountId: string;
  pointsBalance: number;
  totalPointsEarned: number;
  quests: EventQuestProgress[];
  updatedAt: string | null;
};

export type EventRewardClaim = {
  id: string;
  eventId: string;
  rewardId: string;
  rewardCode: string;
  accountId: string;
  pointsSpent: number;
  status: ClaimStatus;
  failureReason: string | null;
  createdAt: string;
  completedAt: string | null;
};

export type EventDetails = {
  event: GameEvent;
  progress: EventProgress;
  claims: EventRewardClaim[];
};

export type EventQuestRequest = {
  code: string;
  displayName: string;
  description: string | null;
  kind: QuestKind;
  metric: QuestMetric;
  target: number;
  points: number;
  sortOrder: number;
};

export type EventRewardRequest = {
  code: string;
  displayName: string;
  description: string | null;
  pointsCost: number;
  catalogItemId: string;
  itemCode: string;
  itemKind: string;
  slot: string | null;
  quantity: number;
  sortOrder: number;
  isRepeatable: boolean;
};

export type CreateEventRequest = {
  code: string;
  displayName: string;
  description: string | null;
  kind: EventKind;
  currencyCode: string;
  announceAt: string | null;
  startsAt: string;
  endsAt: string;
  graceEndsAt: string | null;
  quests: EventQuestRequest[];
  rewards: EventRewardRequest[];
};

export type UpdateEventRequest = Omit<CreateEventRequest, "code">;
