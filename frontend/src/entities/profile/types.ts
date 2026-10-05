// Зеркалит ProfileService.Contracts.Dtos.ProfileDto (camelCase).

export type PlayerProfileResponse = {
  id: string;
  nickname: string;
  region: string | null;
  createdAt: string;
};
