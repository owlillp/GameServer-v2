export const routes = {
  home: "/",
  login: "/login",
  register: "/register",
  profile: "/profile",
  dashboard: "/dashboard",
  admin: "/admin",
  adminUsers: "/admin/users",
  adminWallets: "/admin/wallets",
  adminPlayer: (walletId: string) => `/admin/players/${walletId}`,
} as const;

export type RouteKey = keyof typeof routes;
