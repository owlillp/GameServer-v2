// Зеркалит контракты AuthService.Contracts (.NET PropertyNamingPolicy = camelCase).

export type RegisterRequest = {
  email: string;
  userName: string;
  password: string;
};

export type RegisterResponse = {
  accountId: string;
};

export type LoginRequest = {
  email: string;
  password: string;
};

// POST /auth/login — ставит Identity-cookie (нужна для шага /connect/authorize).
export type LoginResponse = {
  accountId: string;
  email: string;
  userName: string;
};

// GET /auth/me — только identity-поля. Игровой профиль (ник, регион) живёт
// в ProfileService и запрашивается отдельно через /profiles/me.
export type ProfileResponse = {
  id: string;
  email: string | null;
  username: string | null;
  createdAt: string;
  updatedAt: string;
};
