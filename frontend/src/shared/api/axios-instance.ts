import axios, { AxiosHeaders } from "axios";
import { tokenStorage } from "@/src/shared/auth/token-storage";
import { Envelope } from "./envelope";
import { EnvelopeError } from "./errors";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL;

export const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: { "Content-Type": "application/json" },
});

// Если есть access-токен — добавляем Authorization: Bearer.
apiClient.interceptors.request.use((config) => {
  const token = tokenStorage.get();

  if (token) {
    config.headers = AxiosHeaders.from(config.headers);
    config.headers.set("Authorization", `Bearer ${token}`);
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => {
    const envelope = response.data as Envelope;

    if (envelope?.isError && envelope.error) {
      return Promise.reject(new EnvelopeError(envelope.error));
    }

    return response;
  },
  (error) => {
    if (axios.isAxiosError(error)) {
      // Токен недействителен — сбрасываем, чтобы не слать его дальше.
      if (error.response?.status === 401) {
        tokenStorage.clear();
      }

      const envelope = error.response?.data as Envelope | undefined;

      if (envelope?.isError && envelope.error) {
        return Promise.reject(new EnvelopeError(envelope.error));
      }
    }

    return Promise.reject(error);
  },
);
