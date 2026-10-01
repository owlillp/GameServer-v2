const TOKEN_KEY = "template-service.access-token";

// Access-токен AuthService для вызовов API. Хранится в localStorage и
// подставляется в Authorization header интерсептором axios.
export const tokenStorage = {
  get(): string | null {
    if (typeof window === "undefined") {
      return null;
    }

    return window.localStorage.getItem(TOKEN_KEY);
  },

  set(token: string): void {
    window.localStorage.setItem(TOKEN_KEY, token.trim());
  },

  clear(): void {
    window.localStorage.removeItem(TOKEN_KEY);
  },
};
