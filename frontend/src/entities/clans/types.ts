export type ClanRole = "LEADER" | "OFFICER" | "MEMBER";

export type Clan = {
  id: string;
  tag: string;
  name: string;
  description: string | null;
  leaderAccountId: string;
  memberCount: number;
  maxMembers: number;
  clanPoints: number;
  status: "ACTIVE" | "DISBANDED";
  createdAt: string;
};

export type ClanMember = {
  accountId: string;
  role: ClanRole;
  joinedAt: string;
};

export type MyClan = {
  clan: Clan;
  myRole: ClanRole;
  members: ClanMember[];
};

export type ClanJoinRequest = {
  id: string;
  clanId: string;
  clanTag: string;
  accountId: string;
  kind: "REQUEST" | "INVITE";
  status: "PENDING" | "ACCEPTED" | "DECLINED" | "CANCELLED";
  createdBy: string;
  createdAt: string;
};

export type CreateClanRequest = {
  tag: string;
  name: string;
  description?: string;
};
