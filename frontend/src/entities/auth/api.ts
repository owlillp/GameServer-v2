import { apiClient } from "@/src/shared/api/axios-instance";
import { Envelope, unwrapEnvelope } from "@/src/shared/api/envelope";
import { queryOptions } from "@tanstack/react-query";
import type {
  LoginRequest,
  LoginResponse,
  ProfileResponse,
  RegisterRequest,
  RegisterResponse,
} from "./types";

export const authApi = {
  register: async (request: RegisterRequest): Promise<RegisterResponse> => {
    const response = await apiClient.post<Envelope<RegisterResponse>>(
      "/auth/register",
      request,
      { skipAuthRefresh: true },
    );

    return unwrapEnvelope(response.data);
  },

  // Ставит Identity-cookie — browser session для последующего /connect/authorize.
  login: async (request: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<Envelope<LoginResponse>>(
      "/auth/login",
      request,
      { skipAuthRefresh: true },
    );

    return unwrapEnvelope(response.data);
  },

  // Здесь skipAuthRefresh НЕ ставим: на 401 интерсептор обновит токен и повторит.
  profile: async (signal?: AbortSignal): Promise<ProfileResponse> => {
    const response = await apiClient.get<Envelope<ProfileResponse>>(
      "/auth/me",
      { signal },
    );

    return unwrapEnvelope(response.data);
  },
};

export const authQueryKeys = {
  all: ["auth"] as const,
  profile: () => [...authQueryKeys.all, "profile"] as const,
};

// retry: false — ретраить 401 бессмысленно без действий пользователя.
export const profileQueryOptions = () =>
  queryOptions({
    queryKey: authQueryKeys.profile(),
    queryFn: ({ signal }) => authApi.profile(signal),
    retry: false,
    staleTime: 0,
  });
